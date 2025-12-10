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
/// <param name="client"></param>
public class AudioPlaybackService(AudioQueueService audioQueue, GatewayClient client) : BackgroundService
{
                                      // <GuildId, VoiceClient>
    private readonly ConcurrentDictionary<ulong, VoiceConnection> VoiceConnections = [];

                                      // <GuildId, Task>
    private readonly ConcurrentDictionary<ulong, Task> PlaybackTasks = [];

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            ICollection<ulong> guildsWithQueues = audioQueue.GetGuildsWithPlaybackQueues();

            foreach (ulong guildId in guildsWithQueues)
            {
                if (PlaybackTasks.ContainsKey(guildId))
                {
                    if (PlaybackTasks[guildId].IsCompleted)
                    {
                        PlaybackTasks.Remove(guildId, out _);
                    }
                    else
                    {
                        continue;
                    }
                }

                QueuedAudioTrack? nextTrack = audioQueue.PopNextPlayback(guildId);

                if (nextTrack is not null && nextTrack.DownloadedFilePath is not null)
                {
                    Log.Information("Popped next track for guild {GuildId}: {Track}", guildId, nextTrack.DownloadedFilePath);

                    PlaybackTasks[guildId] = PlayAudioFile(nextTrack.VoiceChannelId, guildId, nextTrack.DownloadedFilePath);
                }
            }

            await Task.Delay(1000, stoppingToken);
        }
    }

    /// <summary>
    ///   Plays an audio file in a voice channel
    /// </summary>
    /// <param name="voiceChannelId"></param>
    /// <param name="guildId"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task PlayAudioFile(ulong voiceChannelId, ulong guildId, string filePath)
    {
        try
        {
            if (!VoiceConnections.TryGetValue(guildId, out VoiceConnection? voiceConnection))
            {
                VoiceClient voiceClient = await client.JoinVoiceChannelAsync(
                    guildId,
                    voiceChannelId,
                    new VoiceClientConfiguration
                    {
                        Logger = new BeepskyVoiceLogger(),
                    });

                await voiceClient.StartAsync();
                await voiceClient.EnterSpeakingStateAsync(new SpeakingProperties(SpeakingFlags.Microphone));
                Stream outStream = voiceClient.CreateOutputStream();
                OpusEncodeStream stream = new(outStream, PcmFormat.Short, VoiceChannels.Stereo, OpusApplication.Audio);
                voiceConnection = new VoiceConnection
                {
                    VoiceClient = voiceClient,
                    OutStream = outStream,
                    OpusEncodeStream = stream
                };

                VoiceConnections[guildId] = voiceConnection;
            }

            // TODO: Deal with changing voice channels at some point

            if (!File.Exists(filePath))
            {
                Log.Error("Audio file not found: {Link}", filePath);
            }

            List<string> arguments = [
                "-i", filePath,
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

            Process ffmpeg = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start FFmpeg process");
            await ffmpeg.StandardOutput.BaseStream.CopyToAsync(voiceConnection.OpusEncodeStream);
            string ffmpegErrors = await ffmpeg.StandardError.ReadToEndAsync();
            await voiceConnection.OpusEncodeStream.FlushAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error playing track {Track} in guild {GuildId}", filePath, guildId);
        }
    }
}

// Temporarily here until I figure out if this works
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