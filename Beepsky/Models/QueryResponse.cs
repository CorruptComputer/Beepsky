using System.Text.Json.Serialization;

namespace Beepsky.Models;

/// <summary>
///   Result from a CQRS query.
/// </summary>
/// <typeparam name="TResult"></typeparam>
[Serializable]
[JsonSerializable(typeof(QueryResponse<>))]
public sealed record QueryResponse<TResult> : ResponseBase
{
    private QueryResponse() { }

    /// <summary>
    ///   If the query was successful, this should have some data in it.
    /// </summary>
    public TResult? Result { get; init; }

#pragma warning disable CA1000 // Do not declare static members on generic types
    /// <summary>
    ///   Creates a failure response, optionally with the reason why it failed.
    /// </summary>
    /// <param name="failureReason"></param>
    /// <returns></returns>
    public static QueryResponse<TResult> Fail(string? failureReason = null) => new()
    {
        Success = false,
        FailReason = failureReason
    };
#pragma warning restore CA1000 // Do not declare static members on generic types

    /// <summary>
    ///   Translates a QueryResponse&lt;TResult&gt; into a TResult?
    /// </summary>
    /// <param name="response">The QueryResponse&lt;TResult&gt;</param>
    /// <returns>The newly translated TResult?</returns>
    public static implicit operator TResult?(QueryResponse<TResult> response)
    {
        if (!response.Success || response.Result is null)
        {
            return default;
        }

        return response.Result;
    }

    /// <summary>
    ///   Translates a TResult? into a QueryResponse&lt;TResult&gt;
    /// </summary>
    /// <param name="result">The TResult? you want to translate</param>
    /// <returns>The newly translated QueryResponse&lt;TResult&gt;</returns>
    public static implicit operator QueryResponse<TResult>(TResult? result)
    {
        if (result is null)
        {
            return new()
            {
                Success = false,
                FailReason = "Result is null"
            };
        }

        return new()
        {
            Success = true,
            Result = result
        };
    }
}
