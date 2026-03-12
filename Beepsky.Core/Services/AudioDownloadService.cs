using Beepsky.Core.Features.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Beepsky.Core.Services;

/// <summary>
///   Service that handles audio downloads.
///   This is a background service to allow this to happen in a different thread than the discord events
/// </summary>
/// <param name="audioQueue"></param>
/// <param name="serviceScopeFactory"></param>
public class AudioDownloadService(AudioQueueService audioQueue, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (QueuedAudioTrack nextTrackToDownload in audioQueue.DownloadChannelReader.ReadAllAsync(stoppingToken))
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            ISender scopedSender = scope.ServiceProvider.GetRequiredService<ISender>();

            if (nextTrackToDownload.CancellationTokenSource.IsCancellationRequested)
            {
                Log.Information("Download cancelled before starting for URI {Uri}", nextTrackToDownload.TrackUri);
                audioQueue.RemoveTrack(nextTrackToDownload);
                continue;
            }

            nextTrackToDownload.CurrentState = QueuedAudioTrack.State.Downloading;

            try
            {
                Log.Information("Starting download for track: {TrackUri}", nextTrackToDownload.TrackUri);
                await scopedSender.Send(new DownloadAudioTrack.Command(nextTrackToDownload), nextTrackToDownload.CancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in AudioDownloadService for track {Track}", nextTrackToDownload.TrackUri);
            }
            finally
            {
                // No matter what, ensure the state is set to QueuedForPlayback at the end of this, otherwise the
                // track can get stuck in Downloading state forever. If it fails somehow, the queue service will
                // handle removing it.
                nextTrackToDownload.CurrentState = QueuedAudioTrack.State.QueuedForPlayback;
                audioQueue.SignalPlaybackReady();
            }
        }
    }
}
