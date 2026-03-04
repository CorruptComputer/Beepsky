using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Beepsky.Database.DbSets;
using Beepsky.Database.Operations.AudioDownloads;
using Beepsky.Exceptions;
using Beepsky.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Beepsky.Services;

/// <summary>
///   Service that handles audio playback in voice channels
///   This is a background service to allow this to happen in a different thread than the discord events
/// </summary>
/// <param name="audioQueue"></param>
/// <param name="beepskyConfiguration"></param>
/// <param name="serviceScopeFactory"></param>
public class AudioDownloadService(AudioQueueService audioQueue, BeepskyConfiguration beepskyConfiguration, IServiceScopeFactory serviceScopeFactory) : BackgroundService
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
                switch (nextTrackToDownload.Type)
                {
                    case QueuedAudioTrack.DownloadType.YouTube:
                        await DownloadYouTubeAudioAsync(nextTrackToDownload, scopedSender);
                        break;

                    case QueuedAudioTrack.DownloadType.SoundCloud:
                        await DownloadSoundCloudAudioAsync(nextTrackToDownload, scopedSender);
                        break;

                    default:
                        Log.Warning("Unimplemented download for track type: {Type}", nextTrackToDownload.Type);
                        break;
                }
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

    /// <summary>
    ///   Downloads audio from a YouTube URI using yt-dlp
    /// </summary>
    /// <param name="track"></param>
    /// <param name="sender"></param>
    /// <returns>The file path for the downloaded audio</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task DownloadYouTubeAudioAsync(QueuedAudioTrack track, ISender sender)
    {
        if (track.CancellationTokenSource.IsCancellationRequested)
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
                dbEntry = await sender.Send(new GetAudioDownloadByUrl.Command(downloadUrl), track.CancellationTokenSource.Token);
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
                await SaveDbEntryForTrackAsync(track, downloadUrl, sender, dbEntry);
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
                    await SaveDbEntryForTrackAsync(track, downloadUrl, sender, dbEntry);
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

                if (!track.CancellationTokenSource.IsCancellationRequested)
                {
                    await track.CancellationTokenSource.CancelAsync();
                }

                ytdlp.Kill();
                ytdlp.Dispose();
            }

            return;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error downloading audio {Uri}", track.TrackUri);

            if (!track.CancellationTokenSource.IsCancellationRequested)
            {
                await track.CancellationTokenSource.CancelAsync();
            }
        }
    }

    private static async Task SaveDbEntryForTrackAsync(QueuedAudioTrack track, string downloadUrl, ISender sender, AudioDownload? existingEntry)
    {
        if (track.DownloadedFilePath is null)
        {
            return;
        }

        try
        {
            if (existingEntry is not null)
            {
                existingEntry.FileRemoved = false;
                existingEntry.FilePath = track.DownloadedFilePath;
                if (!string.IsNullOrEmpty(track.Title))
                {
                    existingEntry.Title = track.Title;
                }
                existingEntry.LastAccessedAt = DateTimeOffset.UtcNow;
                await sender.Send(new UpdateAudioDownload.Command(existingEntry), CancellationToken.None);
                Log.Information("Updated audio download metadata in DB for {Uri}", downloadUrl);
            }
            else
            {
                AudioDownload newEntry = new()
                {
                    DownloadUrl = downloadUrl,
                    FilePath = track.DownloadedFilePath,
                    Title = track.Title ?? string.Empty,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastAccessedAt = DateTimeOffset.UtcNow,
                    PlayCount = 0
                };

                await sender.Send(new CreateAudioDownload.Command(newEntry), CancellationToken.None);
                Log.Information("Saved audio download metadata to DB for {Uri}", downloadUrl);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving audio download metadata to DB for {Uri}", downloadUrl);
        }
    }

    /// <summary>
    ///   Downloads audio from a SoundCloud URI using yt-dlp
    /// </summary>
    /// <param name="track"></param>
    /// <param name="sender"></param>
    /// <returns></returns>
    private async Task DownloadSoundCloudAudioAsync(QueuedAudioTrack track, ISender sender)
    {
        if (track.CancellationTokenSource.IsCancellationRequested)
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
                dbEntry = await sender.Send(new GetAudioDownloadByUrl.Command(downloadUrl), track.CancellationTokenSource.Token);
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

            string outputDir = Path.Join(beepskyConfiguration.DownloadCache, "sc");

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string fileHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(downloadUrl)));
            string? outputFilePath = Path.Join(outputDir, $"{fileHash}.mp3");
            if (File.Exists(outputFilePath))
            {
                Log.Information("Audio file already exists: {FilePath}", outputFilePath);
                track.DownloadedFilePath = outputFilePath;
                await GetMetadataForSoundCloudTrackAsync(track);
                await SaveDbEntryForTrackAsync(track, downloadUrl, sender, dbEntry);
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
                    track.DownloadedFilePath = outputFilePath;
                    await GetMetadataForSoundCloudTrackAsync(track);
                    await SaveDbEntryForTrackAsync(track, downloadUrl, sender, dbEntry);
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

                if (!track.CancellationTokenSource.IsCancellationRequested)
                {
                    await track.CancellationTokenSource.CancelAsync();
                }

                ytdlp.Kill();
                ytdlp.Dispose();
            }

            return;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error downloading audio {Uri}", track.TrackUri);

            if (!track.CancellationTokenSource.IsCancellationRequested)
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
                // This part can fail without breaking the download, so fine to mostly ignore this error
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
