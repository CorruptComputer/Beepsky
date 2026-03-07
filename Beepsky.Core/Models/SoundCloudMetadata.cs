using System.Text.Json.Serialization;

namespace Beepsky.Core.Models;

/// <summary>
///   Represents metadata for a SoundCloud track
/// </summary>
public sealed class SoundCloudMetadata
{
    /// <summary>
    ///   The title of the track
    /// </summary>
    [JsonPropertyName("title")]
    public string? TrackTitle { get; set; }

    /// <summary>
    ///   The duration of the track in seconds
    /// </summary>
    [JsonPropertyName("duration")]
    public double? DurationInSeconds { get; set; }

}
