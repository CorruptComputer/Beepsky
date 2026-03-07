using System.Text.Json.Serialization;

namespace Beepsky.Core.Models;

/// <summary>
///   Basic response of any request.
/// </summary>
[Serializable]
[JsonSerializable(typeof(ResponseBase))]
public record ResponseBase
{
    /// <summary>
    ///   Base constructor for this response base
    /// </summary>
    protected ResponseBase() { }

    /// <summary>
    ///   Was the command successful?
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///   Optional even if the command failed, but maybe the reason why it failed.
    /// </summary>
    public string? FailReason { get; init; }
}