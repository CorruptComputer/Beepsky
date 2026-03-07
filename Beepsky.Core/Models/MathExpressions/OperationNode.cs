using Beepsky.Core.Exceptions;

namespace Beepsky.Core.Models.MathExpressions;

/// <summary>
///   Represents an operation in an expression.
/// </summary>
public sealed class OperationNode(OperatorKind op, ExpressionNode left, ExpressionNode? right, double value)
    : ExpressionNode(value)
{
    /// <summary>
    ///   The operator of this operation.
    /// </summary>
    public OperatorKind Operator { get; } = op;

    /// <summary>
    ///   The left operand of this operation.
    /// </summary>
    public ExpressionNode Left { get; } = left;

    /// <summary>
    ///   The right operand of this operation, if any.
    /// </summary>
    public ExpressionNode? Right { get; } = right;

    /// <summary>
    ///   Operator precedence (PEMDAS).
    /// </summary>
    public override int Precedence => Operator switch
    {
        OperatorKind.Add or OperatorKind.Subtract => 1,
        OperatorKind.Multiply or OperatorKind.Divide => 2,
        OperatorKind.Power => 3,
        OperatorKind.Factorial or OperatorKind.AbsoluteValue => 4,
        _ => throw new BeepskyException("Unknown operator kind.")
    };

    /// <summary>
    ///   Returns a correctly parenthesized string representation.
    /// </summary>
    /// <returns></returns>
    public override string ToExpressionString()
    {
        return Operator switch
        {
            OperatorKind.Factorial =>
                $"{FormatLeftOperand(Left)}!",

            OperatorKind.AbsoluteValue =>
                $"|{Left.ToExpressionString()}|",

            OperatorKind.Power =>
                $"{FormatLeftOperand(Left)}^{FormatRightOperand(Right!)}",

            _ =>
                $"{FormatLeftOperand(Left)} {GetSymbol()} {FormatRightOperand(Right!)}"
        };
    }

    private string FormatLeftOperand(ExpressionNode node)
    {
        // Left operand needs parens if lower precedence,
        // OR if same precedence and operator is right-associative (power)
        bool needsParens = node.Precedence < Precedence
                           || (node.Precedence == Precedence && Operator == OperatorKind.Power);

        return needsParens
            ? $"({node.ToExpressionString()})"
            : node.ToExpressionString();
    }

    private string FormatRightOperand(ExpressionNode node)
    {
        // Right operand needs parens if lower precedence,
        // OR if same precedence and operator is left-associative & non-commutative (subtract/divide)
        bool needsParens = node.Precedence < Precedence
                           || (node.Precedence == Precedence && Operator is OperatorKind.Subtract or OperatorKind.Divide);

        return needsParens
            ? $"({node.ToExpressionString()})"
            : node.ToExpressionString();
    }

    private string GetSymbol() => Operator switch
    {
        OperatorKind.Add => "+",
        OperatorKind.Subtract => "-",
        OperatorKind.Multiply => "*",
        OperatorKind.Divide => "/",
        _ => throw new BeepskyException("Unknown operator kind.")
    };
}
