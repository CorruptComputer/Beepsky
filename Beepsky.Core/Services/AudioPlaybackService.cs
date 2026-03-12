using Beepsky.Core.Features.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Beepsky.Core.Services;

/// <summary>
///   Service that handles audio playback.
///   This is a background service to allow this to happen in a different thread than the discord events
/// </summary>
/// <param name="audioQueue"></param>
/// <param name="discordIntegrationService"></param>
/// <param name="serviceScopeFactory"></param>
public class AudioPlaybackService(AudioQueueService audioQueue, IDiscordIntegrationService discordIntegrationService, IServiceScopeFactory serviceScopeFactory) : BackgroundService
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

                await audioQueue.WaitForPlaybackSignalAsync(stoppingToken);
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
                    await discordIntegrationService.DisconnectFromGuildVoiceConnectionAsync(guildId, cancellationToken);
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
                await scopedSender.Send(new PlayAudioTrack.Command(nextTrack), nextTrack.CancellationTokenSource.Token);
            }, nextTrack.CancellationTokenSource.Token);

            // Wake the playback loop when this track finishes (for any reason) so the next track starts promptly
            _ = nextTrack.PlaybackTask.ContinueWith(
                _ => audioQueue.SignalPlaybackReady(),
                cancellationToken,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }
    }
}
