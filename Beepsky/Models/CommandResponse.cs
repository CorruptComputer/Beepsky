using System.Text.Json.Serialization;

namespace Beepsky.Models;

/// <summary>
///   Basic response of any DB command.
/// </summary>
[Serializable]
[JsonSerializable(typeof(CommandResponse))]
public sealed record CommandResponse : ResponseBase
{
    private CommandResponse() { }

    /// <summary>
    ///   Creates a successful response, with multiple IDs
    /// </summary>
    /// <returns></returns>
    public static CommandResponse Pass() => new()
    {
        Success = true
    };

    /// <summary>
    ///   Creates a failure response, optionally with the reason why it failed.
    /// </summary>
    /// <param name="failureReason"></param>
    /// <returns></returns>
    public static CommandResponse Fail(string? failureReason = null) => new()
    {
        Success = false,
        FailReason = failureReason
    };
}