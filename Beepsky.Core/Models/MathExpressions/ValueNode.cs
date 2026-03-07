using System.Globalization;

namespace Beepsky.Core.Models.MathExpressions;

/// <summary>
///   Represents a numeric value in an expression.
/// </summary>
public sealed class ValueNode(double value)
    : ExpressionNode(value)
{
    /// <summary>
    ///   Operator precedence (PEMDAS).
    /// </summary>
    public override int Precedence => int.MaxValue;

    /// <summary>
    ///   Returns a correctly parenthesized string representation.
    /// </summary>
    /// <returns></returns>
    public override string ToExpressionString()
    {
        // Round to 10 decimal places to clean up floating-point errors
        double rounded = Math.Round(Value, 10);

        // Format as integer if it's a whole number, otherwise show decimals
        string formatted = rounded == Math.Truncate(rounded)
            ? ((long)rounded).ToString(CultureInfo.InvariantCulture)
            : rounded.ToString("G", CultureInfo.InvariantCulture);

        return Value < 0 ? $"({formatted})" : formatted;
    }
}
