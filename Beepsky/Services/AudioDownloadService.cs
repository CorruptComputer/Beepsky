using System.Diagnostics;
using System.Text.Json;
using Beepsky.Exceptions;
using Beepsky.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Beepsky.Services;

/// <summary>
///   Service that handles audio playback in voice channels
///   This is a background service to allow this to happen in a different thread than the discord events
/// </summary>
/// <param name="audioQueue"></param>
/// <param name="beepskyConfiguration"></param>
public class AudioDownloadService(AudioQueueService audioQueue, BeepskyConfiguration beepskyConfiguration) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                QueuedAudioTrack? nextTrackToDownload = audioQueue.GetNextDownload();

                if (nextTrackToDownload is not null)
                {
                    nextTrackToDownload.CurrentState = QueuedAudioTrack.State.Downloading;

                    try
                    {
                        Log.Information("Starting download for track: {TrackUri}", nextTrackToDownload.TrackUri);
                        switch (nextTrackToDownload.Type)
                        {
                            case QueuedAudioTrack.DownloadType.YouTube:
                                await DownloadYouTubeAudioAsync(nextTrackToDownload);
                                break;

                            default:
                                Log.Warning("Unimplemented download for track type: {Type}", nextTrackToDownload.Type);
                                break;
                        }
                    }
                    // No need to catch here, the outer catch will handle that. This try is just here to have the finally below
                    // No matter what, ensure the state is set to QueuedForPlayback at the end of this, otherwise the track can get stuck in Downloading state forever
                    // If it fails somehow, the queue service will handle removing it
                    finally
                    {
                        nextTrackToDownload.CurrentState = QueuedAudioTrack.State.QueuedForPlayback;
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in AudioDownloadService main loop");
            }
        }
    }

    /// <summary>
    ///   Downloads audio from a YouTube URI using yt-dlp
    /// </summary>
    /// <param name="track"></param>
    /// <returns>The file path for the downloaded audio</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task DownloadYouTubeAudioAsync(QueuedAudioTrack track)
    {
        if (track.CancellationTokenSource.IsCancellationRequested)
        {
            Log.Information("Download cancelled before starting for URI {Uri}", track.TrackUri);
            return;
        }

        try
        {
            string outputDir = Path.Join(beepskyConfiguration.DownloadCache, "yt");

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string videoId = track.TrackUri.Query.Split("v=")[1].Split('&')[0];
            string? outputFilePath = Path.Join(outputDir, $"{videoId}.mp3");

            if (File.Exists(outputFilePath))
            {
                Log.Information("Audio file already exists: {FilePath}", outputFilePath);
                track.DownloadedFilePath = outputFilePath;
                await GetMetadataForYouTubeTrackAsync(track);
                return;
            }

            List<string> arguments = [
                "--output", outputFilePath,
                "-t", "mp3",
                "--write-info-json",
                track.TrackUri.ToString()
            ];

            ProcessStartInfo startInfo = new("yt-dlp")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (string arg in arguments)
            {
                startInfo.ArgumentList.Add(arg);
            }

            Log.Information("Starting yt-dlp for URI {Uri}", track.TrackUri);
            using Process ytdlp = Process.Start(startInfo)
                ?? throw new BeepskyException("Could not start yt-dlp process");
            try
            {
                await ytdlp.WaitForExitAsync(track.CancellationTokenSource.Token).AwaitWithTimeout(
                TimeSpan.FromMinutes(1),
                onSuccess: async () =>
                {
                    Log.Information("Finished downloading {Track}", outputFilePath);
                    //string ytdlpOutput = await ytdlp.StandardOutput.ReadToEndAsync();
                    //string ytdlpErrors = await ytdlp.StandardError.ReadToEndAsync();

                    track.DownloadedFilePath = outputFilePath;
                    await GetMetadataForYouTubeTrackAsync(track);
                },
                onTimeout: () =>
                {
                    Log.Warning("yt-dlp timed out {Track}", outputFilePath);
                    ytdlp.Kill(entireProcessTree: true);
                    outputFilePath = null;
                    return Task.CompletedTask;
                },
                onComplete: () =>
                {
                    ytdlp.Dispose();
                    return Task.CompletedTask;
                },
                tasksLinkedCts: track.CancellationTokenSource);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error downloading audio {Uri}", track.TrackUri);
                ytdlp.Kill();
                ytdlp.Dispose();
            }

            return;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error downloading audio {Uri}", track.TrackUri);
            return;
        }
    }

    private static async Task GetMetadataForYouTubeTrackAsync(QueuedAudioTrack track)
    {
        string metadataPath = track.DownloadedFilePath + ".info.json";
        if (File.Exists(metadataPath))
        {
            string metadataJson = await File.ReadAllTextAsync(metadataPath, track.CancellationTokenSource.Token);
            if (!string.IsNullOrWhiteSpace(metadataJson))
            {
                // This part can fail without breaking the download, so fine to mostly ignore this error
                try
                {
                    YouTubeMetadata? metadata = JsonSerializer.Deserialize<YouTubeMetadata>(metadataJson);
                    if (metadata is not null)
                    {
                        track.Title = metadata.VideoTitle;

                        if (metadata.DurationInSeconds is not null)
                        {
                            track.Duration = TimeSpan.FromSeconds(metadata.DurationInSeconds.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error parsing YouTube metadata JSON for track {TrackUri}", track.TrackUri);
                }
            }
        }
    }
}
