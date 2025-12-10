using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Logging;
using Serilog;

namespace Beepsky.Services;

/// <summary>
///   Service that handles audio playback in voice channels
/// </summary>
/// <param name="audioQueue"></param>
public class AudioDownloadService(AudioQueueService audioQueue) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            QueuedAudioTrack? nextTrackToDownload = audioQueue.PopNextDownload();

            if (nextTrackToDownload is not null)
            {
                Log.Information("Starting download for track: {TrackUri}", nextTrackToDownload.TrackUri);

                switch (nextTrackToDownload.Type)
                {
                    case QueuedAudioTrack.DownloadType.YouTube:
                        string? downloadedFilePath = await DownloadYouTubeAudio(nextTrackToDownload.TrackUri);
                        if (downloadedFilePath is null)
                        {
                            Log.Warning("Failed to download track: {TrackUri}", nextTrackToDownload.TrackUri);
                            break;
                        }

                        nextTrackToDownload.DownloadedFilePath = downloadedFilePath;
                        audioQueue.AddTrackToPlaybackQueue(nextTrackToDownload);
                        break;
                }

            }

            await Task.Delay(1000, stoppingToken);
        }
    }

    /// <summary>
    ///   Downloads audio from a YouTube URI using yt-dlp
    /// </summary>
    /// <param name="uri"></param>
    /// <returns>The file path for the downloaded audio</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static async Task<string?> DownloadYouTubeAudio(Uri uri)
    {
        try
        {
            string videoId = uri.Query.Split("v=")[1].Split('&')[0];

            string outputFilePath = $"/tmp/beepsky/{videoId}.mp3";

            if (File.Exists(outputFilePath))
            {
                Log.Information("Audio file already exists: {FilePath}", outputFilePath);
                return outputFilePath;
            }

            // yt-dlp --output /tmp/beepsky/MBNDp3p1BBw.mp3 -t mp3 https://www.youtube.com/watch?v=MBNDp3p1BBw

            List<string> arguments = [
                "--output", outputFilePath,
                "-t", "mp3",
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

            Process ytdlp = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start yt-dlp process");

            await ytdlp.WaitForExitAsync();

            string ytdlpOutput = await ytdlp.StandardOutput.ReadToEndAsync();
            string ytdlpErrors = await ytdlp.StandardError.ReadToEndAsync();

            return outputFilePath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error downloading youtube audio {Uri}", uri);
            return null;
        }
    }
}
