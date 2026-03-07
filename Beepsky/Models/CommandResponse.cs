using System.Text.Json.Serialization;

namespace Beepsky.Models;

/// <summary>
///   Basic response of any CQRS command.
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

    /// <summary>
    ///   Translates a bool into a CommandResponse, assuming true means the command was successful.
    /// </summary>
    /// <param name="success"></param>
    /// <returns>The newly translated CommandResponse</returns>
    public static implicit operator CommandResponse(bool success)
    {
        return new()
        {
            Success = success
        };
    }

    /// <summary>
    ///   Translates a CommandResponse into a bool, assuming true means the command was successful.
    /// </summary>
    /// <param name="response"></param>
    /// <returns>The newly translated bool</returns>
    public static implicit operator bool(CommandResponse response)
    {
        return response.Success;
    }
}