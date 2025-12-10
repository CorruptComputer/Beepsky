using NetCord.Gateway.Voice;

namespace Beepsky.Models;

internal sealed record VoiceConnection
{
    public required VoiceClient VoiceClient { get; init; }
    public required Stream OutStream { get; init; }
    public required OpusEncodeStream OpusEncodeStream { get; init; }
}
