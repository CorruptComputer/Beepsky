using System.Collections.Concurrent;
using System.Diagnostics;
using Beepsky.Exceptions;
using Beepsky.Extensions;
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
public class AudioPlaybackService(AudioQueueService audioQueue, VoiceConnectionService voiceConnectionService) : BackgroundService
{
    // <GuildId, Task>
    private readonly ConcurrentDictionary<ulong, Task> PlaybackTasks = [];

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Need to skip whats currently playing
            IEnumerable<ulong> guildsToSkip = audioQueue.GetGuildsWithSkips();
            foreach (ulong guildId in guildsToSkip)
            {
                VoiceConnection? vc = voiceConnectionService.GetVoiceConnectionForGuild(guildId);
                if (vc is not null)
                {
                    Log.Information("Skip requested for guild {GuildId}, cancelling playback", guildId);
                    try
                    {
                        vc.CurrentlyPlaying?.CancellationTokenSource.Cancel();
                    }
                    catch (ObjectDisposedException) { /* Ignore */ }

                    audioQueue.ClearSkipForGuild(guildId);
                }
            }

            // Play next track
            IEnumerable<ulong> guildsWithQueues = audioQueue.GetGuildsWithPlaybackQueues();
            foreach (ulong guildId in guildsWithQueues)
            {
                QueuedAudioTrack? nextTrack = audioQueue.GetNextPlayback(guildId);
                while (nextTrack is not null && nextTrack.CancellationTokenSource.IsCancellationRequested)
                {
                    Log.Information("Skipping cancelled track");
                    audioQueue.RemoveTrack(nextTrack);
                    nextTrack = audioQueue.GetNextPlayback(guildId);
                }

                if (PlaybackTasks.ContainsKey(guildId))
                {
                    // Done playing
                    if (PlaybackTasks[guildId].IsCompleted || PlaybackTasks[guildId].IsCanceled)
                    {
                        PlaybackTasks.Remove(guildId, out _);
                        QueuedAudioTrack? completedTrack = audioQueue.GetCurrentlyPlayingTrackForGuild(guildId);
                        if (completedTrack is not null)
                        {
                            Log.Information("Completed playback for track {Track} in guild {GuildId}", completedTrack.DownloadedFilePath, guildId);
                            audioQueue.RemoveTrack(completedTrack);
                        }

                        // Nothing next
                        if (nextTrack is null)
                        {
                            Log.Information("No tracks left to play for guild {GuildId}", guildId);
                            await voiceConnectionService.DisconnectFromGuildAsync(guildId, stoppingToken);
                            continue;
                        }
                    }
                    // Still playing
                    else
                    {
                        continue;
                    }
                }

                // Something next
                if (nextTrack is not null && nextTrack.DownloadedFilePath is not null)
                {
                    Log.Information("Popped next track for guild {GuildId}: {Track}", guildId, nextTrack.DownloadedFilePath);

                    PlaybackTasks[guildId] = Task.Run(async () =>
                    {
                        nextTrack.CurrentState = QueuedAudioTrack.State.Playing;
                        await PlayAudioFile(nextTrack);
                    }, nextTrack.CancellationTokenSource.Token);
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
        }
    }

    /// <summary>
    ///   Plays an audio file in a voice channel
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task PlayAudioFile(QueuedAudioTrack track)
    {
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

        try
        {
            VoiceConnection voiceConnection = await voiceConnectionService.GetOrCreateVoiceConnectionForTrackAsync(track);

            if (voiceConnection.OpusEncodeStream is null)
            {
                throw new BeepskyException("Invalid state, OpusEncodeStream is null");
            }

            if (!File.Exists(track.DownloadedFilePath))
            {
                Log.Error("Audio file not found: {Link}", track.DownloadedFilePath);
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

            using Process ffmpeg = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start FFmpeg process");
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
                    ffmpeg.Kill();
                    return Task.CompletedTask;
                },
                onComplete: () =>
                {
                    ffmpeg.Dispose();
                    voiceConnection.CurrentlyPlaying = null;
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
            track.CancellationTokenSource.Dispose();
        }
    }
}

internal sealed class BeepskyVoiceLogger : IVoiceLogger
{
    public bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel >= LogLevel.Warning)
        {
            return true;
        }

        return false;
    }

    public void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (exception is not null)
        {
            Serilog.Log.Error(exception, "Voice log [{LogLevel}]: {Message}", logLevel, formatter(state, exception));
        }
    }
}