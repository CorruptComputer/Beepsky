using System.Collections.Specialized;
using System.Web;
using Beepsky.Core.Consts;
using Beepsky.Core.Services;
using Serilog;

namespace Beepsky.Core.Features.Audio;

/// <inheritdoc />
public class QueueTrackInGuild(ISender sender, AudioQueueService audioQueueService, IHttpClientFactory httpClientFactory)
    : IRequestHandler<QueueTrackInGuild.Command, CommandResponse>
{
    /// <summary>
    ///   Command to queue a track for playback in a guild
    /// </summary>
    /// <param name="GuildId"></param>
    /// <param name="VoiceChannelId"></param>
    /// <param name="Track"></param>
    public record Command(ulong GuildId, ulong VoiceChannelId, string Track) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
    {
        bool valid = Uri.TryCreate(request.Track, UriKind.Absolute, out Uri? result);

        if (!valid
            || result is null
            || !result.IsWellFormedOriginalString())
        {
            result = await sender.Send(new SearchYouTubeForTrackUrl.Query(request.Track), cancellationToken);
            if (result is null)
            {
                return CommandResponse.Fail();
            }
        }

        QueuedAudioTrack.DownloadType? type = GetDownloadType(result);
        Uri? normalizedUri = type switch
        {
            QueuedAudioTrack.DownloadType.YouTube => NormalizeYouTubeUrl(result),
            QueuedAudioTrack.DownloadType.SoundCloud => await NormalizeSoundCloudUrlAsync(result),
            _ => null
        };

        if (type is null || normalizedUri is null)
        {
            Log.Warning("Unsupported track URL provided: {Link}", request.Track);
            return CommandResponse.Fail();
        }

        return audioQueueService.QueueTrack(request.VoiceChannelId, request.GuildId, normalizedUri, type.Value);
    }

    private static QueuedAudioTrack.DownloadType? GetDownloadType(Uri uri) => uri.Host switch
    {
        "www.youtube.com" or "youtube.com" or "youtu.be" => QueuedAudioTrack.DownloadType.YouTube,
        "soundcloud.com" or "www.soundcloud.com" or "on.soundcloud.com" => QueuedAudioTrack.DownloadType.SoundCloud,
        _ => null
    };

    private static Uri NormalizeYouTubeUrl(Uri uri)
    {
        if (uri.Host is "youtu.be")
        {
            uri = new UriBuilder("https", "www.youtube.com")
            {
                Path = "/watch",
                Query = $"v={uri.AbsolutePath.TrimStart('/')}"
            }.Uri;
        }

        // Remove all query parameters except for "v="
        NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
        string? v = query["v"];
        uri = new UriBuilder(uri)
        {
            Query = $"v={(string.IsNullOrEmpty(v) ? "dQw4w9WgXcQ" : v)}"
        }.Uri;

        return uri;
    }

    private async Task<Uri?> NormalizeSoundCloudUrlAsync(Uri uri)
    {
        // Resolve short links to the canonical URL
        if (uri.Host is "on.soundcloud.com")
        {
            try
            {
                HttpClient httpClient = httpClientFactory.CreateClient(HttpClientNames.AudioSourceLookup);
                HttpResponseMessage response = await httpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Head, uri)
                );

                string? location = response.Headers.Location?.ToString();
                if (string.IsNullOrEmpty(location) || !Uri.TryCreate(location, UriKind.Absolute, out Uri? resolved))
                {
                    Log.Warning("Could not resolve SoundCloud short link: {Uri}", uri);
                    return null;
                }

                uri = resolved;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to resolve SoundCloud short link: {Uri}", uri);
                return null;
            }
        }

        // Strip www. and all query params
        uri = new UriBuilder("https", "soundcloud.com")
        {
            Path = uri.AbsolutePath,
            Query = string.Empty
        }.Uri;

        return uri;
    }
}
