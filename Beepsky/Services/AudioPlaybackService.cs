using System.Diagnostics;
using Beepsky.Database.DbSets;
using Beepsky.Database.Operations.AudioDownloads;
using Beepsky.Exceptions;
using Beepsky.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCord.Logging;
using Serilog;

namespace Beepsky.Services;

/// <summary>
///   Service that handles audio playback in voice channels
///   This is a background service to allow this to happen in a different thread than the discord events
/// </summary>
/// <param name="audioQueue"></param>
/// <param name="voiceConnectionService"></param>
/// <param name="serviceScopeFactory"></param>
public class AudioPlaybackService(AudioQueueService audioQueue, VoiceConnectionService voiceConnectionService, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Need to skip whats currently playing
                IEnumerable<ulong> guildsToSkip = audioQueue.GetGuildsWithSkips();
                foreach (ulong guildId in guildsToSkip)
                {
                    SkipCurrentlyPlayingTrack(guildId);
                }

                // Play next track
                IEnumerable<ulong> guildsWithQueues = audioQueue.GetGuildsWithPlaybackQueues();
                foreach (ulong guildId in guildsWithQueues)
                {
                    await ProcessQueueForGuildAsync(guildId, stoppingToken);
                }

                await audioQueue.PlaybackWakeSignal.WaitAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in AudioPlaybackService main loop");
            }
        }
    }

    private void SkipCurrentlyPlayingTrack(ulong guildId)
    {
        Log.Information("Skip requested for guild {GuildId}, cancelling playback", guildId);

        QueuedAudioTrack? track = audioQueue.GetCurrentlyPlayingTrackForGuild(guildId);
        if (track is not null)
        {
            try
            {
                track.CancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException) { /* Ignore */ }
        }

        audioQueue.ClearSkipForGuild(guildId);
    }

    private async Task ProcessQueueForGuildAsync(ulong guildId, CancellationToken cancellationToken)
    {
        // Get next track, skipping cancelled ones
        QueuedAudioTrack? nextTrack = audioQueue.GetNextPlayback(guildId);
        while (nextTrack?.CancellationTokenSource.IsCancellationRequested == true)
        {
            Log.Information("Next track {Track} in guild {GuildId} has been cancelled, removing from queue", nextTrack.DownloadedFilePath, guildId);
            audioQueue.RemoveTrack(nextTrack);
            nextTrack = audioQueue.GetNextPlayback(guildId);
        }

        QueuedAudioTrack? currentlyPlaying = audioQueue.GetCurrentlyPlayingTrackForGuild(guildId);
        if (currentlyPlaying is not null)
        {
            if (currentlyPlaying.CancellationTokenSource.IsCancellationRequested)
            {
                Log.Information("Currently playing track {Track} in guild {GuildId} has been cancelled, removing from queue", currentlyPlaying.DownloadedFilePath, guildId);
                audioQueue.RemoveTrack(currentlyPlaying);
            }
        }

        if (currentlyPlaying is not null)
        {
            if (currentlyPlaying.PlaybackTask is null)
            {
                Log.Warning("Currently playing track {Track} in guild {GuildId} has null PlaybackTask, removing from queue", currentlyPlaying.DownloadedFilePath, guildId);
                audioQueue.RemoveTrack(currentlyPlaying);
                return;
            }

            // Done playing
            if (currentlyPlaying.PlaybackTask.IsCompleted
                || currentlyPlaying.PlaybackTask.IsCanceled
                || currentlyPlaying.PlaybackTask.IsFaulted
                || currentlyPlaying.CancellationTokenSource.IsCancellationRequested)
            {
                audioQueue.RemoveTrack(currentlyPlaying);

                // Nothing next
                if (nextTrack is null)
                {
                    Log.Information("No tracks left to play for guild {GuildId}", guildId);
                    await voiceConnectionService.DisconnectFromGuildAsync(guildId, cancellationToken);
                    return;
                }
            }
            // Still playing, do nothing and return
            else
            {
                return;
            }
        }

        // Something next
        if (nextTrack is not null
            && nextTrack.DownloadedFilePath is not null)
        {
            Log.Information("Popped next track for guild {GuildId}: {Track}", guildId, nextTrack.DownloadedFilePath);

            nextTrack.PlaybackTask = Task.Run(async () =>
            {
                using IServiceScope scope = serviceScopeFactory.CreateScope();
                ISender scopedSender = scope.ServiceProvider.GetRequiredService<ISender>();

                nextTrack.CurrentState = QueuedAudioTrack.State.Playing;
                await PlayAudioFile(nextTrack, scopedSender, cancellationToken);
            }, nextTrack.CancellationTokenSource.Token);

            // Wake the playback loop when this track finishes (for any reason) so the next track starts promptly
            _ = nextTrack.PlaybackTask.ContinueWith(
                _ => audioQueue.SignalPlaybackReady(),
                cancellationToken,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }
    }

    private async Task PlayAudioFile(QueuedAudioTrack track, ISender sender, CancellationToken botCancellationToken)
    {
        Log.Information("Playback task started for track {Track} in guild {GuildId}", track.DownloadedFilePath, track.GuildId);

        if (track.DownloadedFilePath is null)
        {
            Log.Error("Attempted to play track with null file path");
            return;
        }

        if (track.CancellationTokenSource.IsCancellationRequested)
        {
            Log.Information("Playback cancelled before starting for track {Track} in guild {GuildId}", track.DownloadedFilePath, track.GuildId);
            return;
        }

        bool requeuedForDownload = false;

        try
        {
            VoiceConnection voiceConnection = await voiceConnectionService.GetOrCreateVoiceConnectionForTrackAsync(track, botCancellationToken);

            if (voiceConnection.OpusEncodeStream is null)
            {
                throw new BeepskyException("Invalid state, OpusEncodeStream is null");
            }

            if (!File.Exists(track.DownloadedFilePath))
            {
                Log.Warning("Audio file missing at playback time, requeueing for re-download: {FilePath}", track.DownloadedFilePath);
                try
                {
                    AudioDownload? dbEntry = await sender.Send(new GetAudioDownloadByUrl.Command(track.TrackUri.ToString()), track.CancellationTokenSource.Token);
                    if (dbEntry is not null)
                    {
                        dbEntry.FileRemoved = true;
                        await sender.Send(new UpdateAudioDownload.Command(dbEntry), track.CancellationTokenSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error setting FileRemoved flag for track {TrackUri}", track.TrackUri);
                }

                audioQueue.RequeueTrackForDownload(track);
                requeuedForDownload = true;
                return;
            }

            try
            {
                AudioDownload? dbEntry = await sender.Send(new GetAudioDownloadByUrl.Command(track.TrackUri.ToString()), CancellationToken.None);
                if (dbEntry is not null)
                {
                    dbEntry.PlayCount++;
                    dbEntry.LastAccessedAt = DateTimeOffset.UtcNow;
                    await sender.Send(new UpdateAudioDownload.Command(dbEntry), CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating play count for track {Track}", track.DownloadedFilePath);
            }

            List<string> arguments = [
                "-i", track.DownloadedFilePath,
                "-loglevel", "-8",
                "-ac", "2",
                "-f", "s16le",
                "-ar", "48000",
                "pipe:1"
            ];

            ProcessStartInfo startInfo = new("ffmpeg")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (string arg in arguments)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using Process ffmpeg = Process.Start(startInfo)
                ?? throw new BeepskyException("Could not start FFmpeg process");

            try
            {
                await ffmpeg.StandardOutput.BaseStream.CopyToAsync(voiceConnection.OpusEncodeStream, track.CancellationTokenSource.Token).AwaitWithTimeout(
                track.Duration ?? TimeSpan.FromMinutes(10),
                onSuccess: async () =>
                {
                    Log.Information("Finished playing track {Track} in guild {GuildId}", track.DownloadedFilePath, track.GuildId);
                    string ffmpegErrors = await ffmpeg.StandardError.ReadToEndAsync();
                    await voiceConnection.OpusEncodeStream.FlushAsync();
                },
                onTimeout: () =>
                {
                    Log.Warning("FFmpeg process timed out for track {Track} in guild {GuildId}", track.DownloadedFilePath, track.GuildId);
                    ffmpeg.Kill(entireProcessTree: true);
                    return Task.CompletedTask;
                },
                onComplete: () =>
                {
                    ffmpeg.Dispose();
                    return Task.CompletedTask;
                },
                tasksLinkedCts: track.CancellationTokenSource);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during playback of track {Track} in guild {GuildId}", track.DownloadedFilePath, track.GuildId);
                ffmpeg.Kill();
                ffmpeg.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error playing track {Track} in guild {GuildId}", track.DownloadedFilePath, track.GuildId);
        }
        finally
        {
            // If it was requeued, we'll still need it.
            if (!requeuedForDownload)
            {
                track.CancellationTokenSource.Dispose();
            }
        }

        Log.Information("Playback task completed for track {Track} in guild {GuildId}", track.DownloadedFilePath, track.GuildId);
    }
}

internal sealed class BeepskyVoiceLogger : IVoiceLogger
{
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Warning;
    }

    public void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (exception is not null)
        {
            Serilog.Log.Error(exception, "Voice log [{LogLevel}]: {Message}", logLevel, formatter(state, exception));
        }
    }
}