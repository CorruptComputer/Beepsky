namespace Beepsky.Core.Consts;

/// <summary>
///   Named HTTP client keys for use with <see cref="System.Net.Http.IHttpClientFactory"/>
/// </summary>
public static class HttpClientNames
{
    /// <summary>
    ///   HTTP client used for audio source URL resolution (YouTube search scraping, SoundCloud short-link resolution).
    ///   Configured with <c>AllowAutoRedirect = false</c> so redirect Location headers can be inspected directly.
    /// </summary>
    public const string AudioSourceLookup = "AudioSourceLookup";
}
