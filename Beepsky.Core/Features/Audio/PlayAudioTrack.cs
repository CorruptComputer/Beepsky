using System.Diagnostics;
using Beepsky.Core.Database.Operations.AudioDownloads;
using Beepsky.Core.Exceptions;
using Beepsky.Core.Extensions;
using Beepsky.Core.Services;
using Serilog;

namespace Beepsky.Core.Features.Audio;

/// <inheritdoc />
public class PlayAudioTrack(IDiscordIntegrationService discordIntegrationService, AudioQueueService audioQueue, ISender sender)
    : IRequestHandler<PlayAudioTrack.Command, CommandResponse>
{
    /// <summary>
    ///   Command to play a downloaded audio track in the guild's voice channel
    /// </summary>
    /// <param name="Track"></param>
    public record Command(QueuedAudioTrack Track) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command command, CancellationToken cancellationToken)
    {
        QueuedAudioTrack track = command.Track;
        Log.Information("Playback task started for track {Track} in guild {GuildId}", track.DownloadedFilePath, track.GuildId);

        if (track.DownloadedFilePath is null)
        {
            Log.Error("Attempted to play track with null file path");
            return CommandResponse.Fail();
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Log.Information("Playback cancelled before starting for track {Track} in guild {GuildId}", track.DownloadedFilePath, track.GuildId);
            return CommandResponse.Fail();
        }

        bool requeuedForDownload = false;

        try
        {
            Stream? opusEncodeStream = await discordIntegrationService.GetOrCreateVoiceConnectionStreamAsync(track.GuildId, track.VoiceChannelId, cancellationToken);
            if (opusEncodeStream is null)
            {
                throw new BeepskyException("Invalid state, opusEncodeStream is null");
            }

            if (!File.Exists(track.DownloadedFilePath))
            {
                Log.Warning("Audio file missing at playback time, requeueing for re-download: {FilePath}", track.DownloadedFilePath);
                await sender.Send(new MarkAudioDownloadFileRemoved.Command(track.TrackUri.ToString()), cancellationToken);
                audioQueue.RequeueTrackForDownload(track);
                requeuedForDownload = true;
                return CommandResponse.Pass();
            }

            await sender.Send(new IncrementAudioDownloadPlayCount.Command(track.TrackUri.ToString()), CancellationToken.None);

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
                await ffmpeg.StandardOutput.BaseStream.CopyToAsync(opusEncodeStream, cancellationToken).AwaitWithTimeout(
                track.Duration ?? TimeSpan.FromMinutes(10),
                onSuccess: async () =>
                {
                    Log.Information("Finished playing track {Track} in guild {GuildId}", track.DownloadedFilePath, track.GuildId);
                    string ffmpegErrors = await ffmpeg.StandardError.ReadToEndAsync();
                    await opusEncodeStream.FlushAsync();
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
            // If it was requeued, we'll still need the CancellationTokenSource
            if (!requeuedForDownload)
            {
                track.CancellationTokenSource.Dispose();
            }
        }

        Log.Information("Playback task completed for track {Track} in guild {GuildId}", track.DownloadedFilePath, track.GuildId);
        return CommandResponse.Pass();
    }
}
