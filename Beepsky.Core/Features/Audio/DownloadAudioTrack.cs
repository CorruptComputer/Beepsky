using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Beepsky.Core.Database.DbSets;
using Beepsky.Core.Database.Operations.AudioDownloads;
using Beepsky.Core.Exceptions;
using Beepsky.Core.Extensions;
using Beepsky.Core.Services;
using Serilog;

namespace Beepsky.Core.Features.Audio;

/// <inheritdoc />
public class DownloadAudioTrack(BeepskyConfiguration beepskyConfiguration, ISender sender)
    : IRequestHandler<DownloadAudioTrack.Command, CommandResponse>
{
    /// <summary>
    ///   Command to download a queued audio track using yt-dlp
    /// </summary>
    /// <param name="Track"></param>
    public record Command(QueuedAudioTrack Track) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command command, CancellationToken cancellationToken)
    {
        QueuedAudioTrack track = command.Track;

        (string outputDir, string outputFilePath, Func<QueuedAudioTrack, Task> getMetadataAsync) = track.Type switch
        {
            QueuedAudioTrack.DownloadType.YouTube => GetYouTubePaths(track),
            QueuedAudioTrack.DownloadType.SoundCloud => GetSoundCloudPaths(track),
            _ => throw new BeepskyException($"Unsupported download type: {track.Type}")
        };

        await DownloadAsync(track, outputDir, outputFilePath, getMetadataAsync, cancellationToken);
        return CommandResponse.Pass();
    }

    private (string OutputDir, string OutputFilePath, Func<QueuedAudioTrack, Task> GetMetadataAsync) GetYouTubePaths(QueuedAudioTrack track)
    {
        string outputDir = Path.Join(beepskyConfiguration.DownloadCache, "yt");
        string videoId = track.TrackUri.Query.Split("v=")[1].Split('&')[0];
        string outputFilePath = Path.Join(outputDir, $"{videoId}.mp3");
        return (outputDir, outputFilePath, GetMetadataForYouTubeTrackAsync);
    }

    private (string OutputDir, string OutputFilePath, Func<QueuedAudioTrack, Task> GetMetadataAsync) GetSoundCloudPaths(QueuedAudioTrack track)
    {
        string downloadUrl = track.TrackUri.ToString();
        string outputDir = Path.Join(beepskyConfiguration.DownloadCache, "sc");
        string fileHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(downloadUrl)));
        string outputFilePath = Path.Join(outputDir, $"{fileHash}.mp3");
        return (outputDir, outputFilePath, GetMetadataForSoundCloudTrackAsync);
    }

    private async Task DownloadAsync(QueuedAudioTrack track, string outputDir, string outputFilePath, Func<QueuedAudioTrack, Task> getMetadataAsync, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            Log.Information("Download cancelled before starting for URI {Uri}", track.TrackUri);
            return;
        }

        try
        {
            string downloadUrl = track.TrackUri.ToString();

            // Check DB cache first
            AudioDownload? dbEntry = null;
            try
            {
                dbEntry = await sender.Send(new GetAudioDownloadByUrl.Command(downloadUrl), cancellationToken);
                if (dbEntry is not null)
                {
                    if (!dbEntry.FileRemoved)
                    {
                        Log.Information("DB cache hit for URI {Uri}, skipping download", track.TrackUri);
                        track.Title = dbEntry.Title;
                        track.DownloadedFilePath = dbEntry.FilePath;
                        return;
                    }

                    Log.Information("DB cache hit for URI {Uri}, but file was removed; re-downloading", track.TrackUri);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking DB cache for track {TrackUri}, proceeding with download", track.TrackUri);
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            if (File.Exists(outputFilePath))
            {
                Log.Information("Audio file already exists: {FilePath}", outputFilePath);
                track.DownloadedFilePath = outputFilePath;
                await getMetadataAsync(track);
                await sender.Send(new UpsertAudioDownload.Command(downloadUrl, outputFilePath, track.Title, dbEntry), CancellationToken.None);
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
                await ytdlp.WaitForExitAsync(cancellationToken).AwaitWithTimeout(
                TimeSpan.FromMinutes(1),
                onSuccess: async () =>
                {
                    Log.Information("Finished downloading {Track}", outputFilePath);
                    track.DownloadedFilePath = outputFilePath;
                    await getMetadataAsync(track);
                    await sender.Send(new UpsertAudioDownload.Command(downloadUrl, outputFilePath, track.Title, dbEntry), CancellationToken.None);
                },
                onTimeout: () =>
                {
                    Log.Warning("yt-dlp timed out {Track}", outputFilePath);
                    ytdlp.Kill(entireProcessTree: true);
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

                if (!cancellationToken.IsCancellationRequested)
                {
                    await track.CancellationTokenSource.CancelAsync();
                }

                ytdlp.Kill();
                ytdlp.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error downloading audio {Uri}", track.TrackUri);

            if (!cancellationToken.IsCancellationRequested)
            {
                await track.CancellationTokenSource.CancelAsync();
            }
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

            try
            {
                File.Delete(metadataPath);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not delete metadata file {MetadataPath}", metadataPath);
            }
        }
    }

    private static async Task GetMetadataForSoundCloudTrackAsync(QueuedAudioTrack track)
    {
        string metadataPath = track.DownloadedFilePath?.Replace(".mp3", string.Empty) + ".info.json";
        if (File.Exists(metadataPath))
        {
            string metadataJson = await File.ReadAllTextAsync(metadataPath, track.CancellationTokenSource.Token);
            if (!string.IsNullOrWhiteSpace(metadataJson))
            {
                try
                {
                    SoundCloudMetadata? metadata = JsonSerializer.Deserialize<SoundCloudMetadata>(metadataJson);
                    if (metadata is not null)
                    {
                        track.Title = metadata.TrackTitle;

                        if (metadata.DurationInSeconds is not null)
                        {
                            track.Duration = TimeSpan.FromSeconds(metadata.DurationInSeconds.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error parsing SoundCloud metadata JSON for track {TrackUri}", track.TrackUri);
                }
            }

            try
            {
                File.Delete(metadataPath);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not delete metadata file {MetadataPath}", metadataPath);
            }
        }
    }
}
