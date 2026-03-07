using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Beepsky.Core.Exceptions;

/// <summary>
///   Exceptions from Beepsky.
/// </summary>
/// <param name="message">What went wrong.</param>
public class BeepskyException(string message) : Exception(message)
{
    /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
    /// <param name="argument">The reference type argument to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
        {
            throw new BeepskyException($"Variable '{paramName}' cannot be null.");
        }
    }
}