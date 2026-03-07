namespace Beepsky.Core.Models.MathExpressions;

/// <summary>
///   Base class for all expression nodes.
/// </summary>
/// <param name="value"></param>
public abstract class ExpressionNode(double value)
{
    /// <summary>
    /// The evaluated value of this expression.
    /// </summary>
    public double Value { get; } = value;

    /// <summary>
    /// Operator precedence (PEMDAS).
    /// Higher number = binds tighter.
    /// </summary>
    public abstract int Precedence { get; }

    /// <summary>
    /// Returns a correctly parenthesized string representation.
    /// </summary>
    public abstract string ToExpressionString();
}
