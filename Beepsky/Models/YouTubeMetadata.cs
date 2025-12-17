using System.Text.Json.Serialization;

namespace Beepsky.Models;

/// <summary>
///   Represents metadata for a YouTube video
/// </summary>
public sealed class YouTubeMetadata
{
    /// <summary>
    ///   The title of the video
    /// </summary>
    [JsonPropertyName("title")]
    public string? VideoTitle { get; set; }

    /// <summary>
    ///   The duration of the video in seconds
    /// </summary>
    [JsonPropertyName("duration")]
    public int? DurationInSeconds { get; set; }

}
