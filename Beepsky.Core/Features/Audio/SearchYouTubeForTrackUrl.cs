using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using Beepsky.Core.Consts;
using Serilog;

namespace Beepsky.Core.Features.Audio;

/// <inheritdoc />
public partial class SearchYouTubeForTrackUrl(IHttpClientFactory httpClientFactory)
    : IRequestHandler<SearchYouTubeForTrackUrl.Query, QueryResponse<Uri>>
{
    /// <summary>
    ///   Command to queue a track for playback in a guild
    /// </summary>
    /// <param name="SearchQuery"></param>
    public record Query(string SearchQuery)
        : IRequest<QueryResponse<Uri>>;

    /// <inheritdoc />
    public async Task<QueryResponse<Uri>> Handle(Query request, CancellationToken cancellationToken)
    {
        Uri searchUrl = new($"https://www.youtube.com/results?search_query={HttpUtility.UrlEncode(request.SearchQuery)}");
        try
        {
            HttpClient httpClient = httpClientFactory.CreateClient(HttpClientNames.AudioSourceLookup);
            HttpResponseMessage response = await httpClient.GetAsync(searchUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("YouTube search returned {StatusCode} for query: {Query}", response.StatusCode, request.SearchQuery);
                return QueryResponse<Uri>.Fail();
            }

            string html = await response.Content.ReadAsStringAsync(cancellationToken);
            Match match = YouTubeVideoIdRegex().Match(html);
            if (!match.Success)
            {
                Log.Warning("No YouTube results found for query: {Query}", request.SearchQuery);
                return QueryResponse<Uri>.Fail();
            }

            string videoId = match.Groups[1].Value;
            return new Uri($"https://www.youtube.com/watch?v={videoId}");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to search YouTube for query: {Query}", request.SearchQuery);
            return QueryResponse<Uri>.Fail();
        }
    }

    [GeneratedRegex("\"videoId\":\"([a-zA-Z0-9_-]+)\"")]
    private static partial Regex YouTubeVideoIdRegex();
}
