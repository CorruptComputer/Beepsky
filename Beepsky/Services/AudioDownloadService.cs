using System.Diagnostics;
using System.Text.Json;
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
            QueuedAudioTrack? nextTrackToDownload = audioQueue.GetNextDownload();

            if (nextTrackToDownload is not null)
            {
                nextTrackToDownload.CurrentState = QueuedAudioTrack.State.Downloading;

                Log.Information("Starting download for track: {TrackUri}", nextTrackToDownload.TrackUri);
                switch (nextTrackToDownload.Type)
                {
                    case QueuedAudioTrack.DownloadType.YouTube:
                        string? downloadedFilePath = await DownloadYouTubeAudio(nextTrackToDownload.TrackUri, nextTrackToDownload.CancellationTokenSource);
                        if (downloadedFilePath is not null)
                        {
                            nextTrackToDownload.DownloadedFilePath = downloadedFilePath;
                            string metadataPath = downloadedFilePath + ".info.json";
                            if (File.Exists(metadataPath))
                            {
                                string metadataJson = await File.ReadAllTextAsync(metadataPath, nextTrackToDownload.CancellationTokenSource.Token);
                                if (!string.IsNullOrWhiteSpace(metadataJson))
                                {
                                    try
                                    {
                                        YouTubeMetadata? metadata = JsonSerializer.Deserialize<YouTubeMetadata>(metadataJson);
                                        if (metadata is not null)
                                        {
                                            nextTrackToDownload.Title = metadata.VideoTitle;

                                            if (metadata.DurationInSeconds is not null)
                                            {
                                                nextTrackToDownload.Duration = TimeSpan.FromSeconds(metadata.DurationInSeconds.Value);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex, "Error parsing YouTube metadata JSON for track {TrackUri}", nextTrackToDownload.TrackUri);
                                    }
                                }
                                // Parse JSON for title and duration
                            }
                        }
                        else
                        {
                            Log.Warning("Failed to download track: {TrackUri}", nextTrackToDownload.TrackUri);

                            try
                            {
                                nextTrackToDownload.CancellationTokenSource.Cancel(); // Yeet
                            }
                            catch (ObjectDisposedException) { /* Ignore */ }
                        }
                        break;

                    default:
                        Log.Warning("Unimplemented download for track type: {Type}", nextTrackToDownload.Type);
                        break;
                }

                nextTrackToDownload.CurrentState = QueuedAudioTrack.State.QueuedForPlayback;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
        }
    }

    /// <summary>
    ///   Downloads audio from a YouTube URI using yt-dlp
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="linkedCts">To be used for the yt-dlp process</param>
    /// <returns>The file path for the downloaded audio</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<string?> DownloadYouTubeAudio(Uri uri, CancellationTokenSource linkedCts)
    {
        if (linkedCts.IsCancellationRequested)
        {
            Log.Information("Download cancelled before starting for URI {Uri}", uri);
            return null;
        }

        try
        {
            string outputDir = Path.Join(beepskyConfiguration.DownloadCache, "yt");

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string videoId = uri.Query.Split("v=")[1].Split('&')[0];
            string? outputFilePath = Path.Join(outputDir, $"{videoId}.mp3");

            if (File.Exists(outputFilePath))
            {
                Log.Information("Audio file already exists: {FilePath}", outputFilePath);
                return outputFilePath;
            }

            List<string> arguments = [
                "--output", outputFilePath,
                "-t", "mp3",
                "--write-info-json",
                uri.ToString()
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

            Log.Information("Starting yt-dlp for URI {Uri}", uri);
            Process ytdlp = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start yt-dlp process");
            try
            {
                await ytdlp.WaitForExitAsync().AwaitWithTimeout(
                TimeSpan.FromMinutes(1),
                onSuccess: async () =>
                {
                    Log.Information("Finished downloading {Track}", outputFilePath);
                    string ytdlpOutput = await ytdlp.StandardOutput.ReadToEndAsync();
                    string ytdlpErrors = await ytdlp.StandardError.ReadToEndAsync();
                },
                onTimeout: () =>
                {
                    Log.Warning("yt-dlp timed out {Track}", outputFilePath);
                    ytdlp.Kill();
                    outputFilePath = null;
                    return Task.CompletedTask;
                },
                onComplete: () =>
                {
                    ytdlp.Dispose();
                    return Task.CompletedTask;
                },
                tasksLinkedCts: linkedCts);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error downloading audio {Uri}", uri);
                ytdlp.Kill();
                ytdlp.Dispose();
            }

            return outputFilePath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error downloading audio {Uri}", uri);
            return null;
        }
    }
}
