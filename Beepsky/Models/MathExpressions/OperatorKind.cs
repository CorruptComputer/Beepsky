namespace Beepsky.Models.MathExpressions;

/// <summary>
///   The kind of operator in an expression.
/// </summary>
public enum OperatorKind
{
    /// <summary>
    ///   Addition operator.
    /// </summary>
    Add,

    /// <summary>
    ///   Subtraction operator.
    /// </summary>
    Subtract,

    /// <summary>
    ///   Multiplication operator.
    /// </summary>
    Multiply,

    /// <summary>
    ///   Division operator.
    /// </summary>
    Divide,

    /// <summary>
    ///   Exponentiation operator.
    /// </summary>
    Power,

    /// <summary>
    ///   Factorial operator.
    /// </summary>
    Factorial,

    /// <summary>
    ///   Absolute value operator.
    /// </summary>
    AbsoluteValue
}
