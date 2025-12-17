using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Beepsky.Models.MathExpressions;

/// <summary>
///   Represents a full expression.
/// </summary>
/// <param name="root"></param>
public sealed class Expression(ExpressionNode root)
{
    /// <summary>
    ///   The root node of the expression.
    /// </summary>
    public ExpressionNode Root { get; } = root;

    /// <summary>
    ///   The evaluated value of this expression.
    /// </summary>
    public double Value => Root.Value;

    /// <summary>
    ///   Returns a correctly parenthesized string representation.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Root.ToExpressionString();

    /// <summary>
    ///   Parses an expression string into an Expression tree.
    /// </summary>
    /// <param name="input">The expression string to parse.</param>
    /// <returns>The parsed Expression.</returns>
    /// <exception cref="FormatException">Thrown when the expression is invalid.</exception>
    public static Expression Parse(string input)
    {
        var parser = new ExpressionParser(input);
        ExpressionNode node = parser.ParseExpression();
        if (!parser.IsAtEnd)
        {
            throw new FormatException($"Unexpected character at position {parser.Position}: '{parser.CurrentChar}'");
        }
        return new Expression(node);
    }

    /// <summary>
    ///   Tries to parse an expression string into an Expression tree.
    /// </summary>
    /// <param name="input">The expression string to parse.</param>
    /// <param name="expression">The parsed expression, or null if parsing failed.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>

    public static bool TryParse(string input, [NotNullWhen(true)] out Expression? expression)
    {
        try
        {
            expression = Parse(input);
            return true;
        }
        catch
        {
            expression = null;
            return false;
        }
    }

    private ref struct ExpressionParser(string input)
    {
        private readonly ReadOnlySpan<char> _input = input.AsSpan();
        private int _position = 0;

        public readonly int Position => _position;
        public readonly bool IsAtEnd => _position >= _input.Length;
        public readonly char CurrentChar => IsAtEnd ? '\0' : _input[_position];

        public ExpressionNode ParseExpression()
        {
            return ParseAddSubtract();
        }

        // Lowest precedence: + and -
        private ExpressionNode ParseAddSubtract()
        {
            ExpressionNode left = ParseMultiplyDivide();

            while (!IsAtEnd)
            {
                SkipWhitespace();
                if (TryConsume('+'))
                {
                    ExpressionNode right = ParseMultiplyDivide();
                    double value = left.Value + right.Value;
                    left = new OperationNode(OperatorKind.Add, left, right, value);
                }
                else if (TryConsume('-'))
                {
                    ExpressionNode right = ParseMultiplyDivide();
                    double value = left.Value - right.Value;
                    left = new OperationNode(OperatorKind.Subtract, left, right, value);
                }
                else
                {
                    break;
                }
            }

            return left;
        }

        // Higher precedence: * and /
        private ExpressionNode ParseMultiplyDivide()
        {
            ExpressionNode left = ParsePower();

            while (!IsAtEnd)
            {
                SkipWhitespace();
                if (TryConsume('*'))
                {
                    ExpressionNode right = ParsePower();
                    double value = left.Value * right.Value;
                    left = new OperationNode(OperatorKind.Multiply, left, right, value);
                }
                else if (TryConsume('/'))
                {
                    ExpressionNode right = ParsePower();
                    double value = left.Value / right.Value;
                    left = new OperationNode(OperatorKind.Divide, left, right, value);
                }
                else
                {
                    break;
                }
            }

            return left;
        }

        // Higher precedence: ^ (right-associative)
        private ExpressionNode ParsePower()
        {
            ExpressionNode left = ParseUnary();

            SkipWhitespace();
            if (TryConsume('^'))
            {
                // Right-associative: parse the right side recursively
                ExpressionNode right = ParsePower();
                double value = Math.Pow(left.Value, right.Value);
                left = new OperationNode(OperatorKind.Power, left, right, value);
            }

            return left;
        }

        // Unary operators and postfix (factorial)
        private ExpressionNode ParseUnary()
        {
            SkipWhitespace();

            // Unary minus
            if (TryConsume('-'))
            {
                ExpressionNode operand = ParseUnary();
                return new ValueNode(-operand.Value);
            }

            // Unary plus (just ignore it)
            if (TryConsume('+'))
            {
                return ParseUnary();
            }

            return ParsePostfix();
        }

        // Postfix operators: factorial (!)
        private ExpressionNode ParsePostfix()
        {
            ExpressionNode node = ParsePrimary();

            while (!IsAtEnd)
            {
                SkipWhitespace();
                if (TryConsume('!'))
                {
                    double value = Factorial(node.Value);
                    node = new OperationNode(OperatorKind.Factorial, node, null, value);
                }
                else
                {
                    break;
                }
            }

            return node;
        }

        // Primary: numbers, parentheses, absolute value
        private ExpressionNode ParsePrimary()
        {
            SkipWhitespace();

            // Parentheses
            if (TryConsume('('))
            {
                ExpressionNode node = ParseExpression();
                SkipWhitespace();
                if (!TryConsume(')'))
                {
                    throw new FormatException($"Expected ')' at position {_position}");
                }
                return node;
            }

            // Absolute value: |expr|
            if (TryConsume('|'))
            {
                ExpressionNode inner = ParseExpression();
                SkipWhitespace();
                if (!TryConsume('|'))
                {
                    throw new FormatException($"Expected closing '|' at position {_position}");
                }
                double value = Math.Abs(inner.Value);
                return new OperationNode(OperatorKind.AbsoluteValue, inner, null, value);
            }

            // Number
            return ParseNumber();
        }

        private ValueNode ParseNumber()
        {
            SkipWhitespace();

            int start = _position;

            // Parse integer part
            while (!IsAtEnd && char.IsDigit(CurrentChar))
            {
                _position++;
            }

            // Parse decimal part if present
            if (!IsAtEnd && CurrentChar == '.')
            {
                _position++;
                while (!IsAtEnd && char.IsDigit(CurrentChar))
                {
                    _position++;
                }
            }

            if (start == _position)
            {
                throw new FormatException($"Expected number at position {_position}, got '{CurrentChar}'");
            }

            ReadOnlySpan<char> numberSpan = _input[start.._position];
            double value = double.Parse(numberSpan, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
            return new(value);
        }

        private void SkipWhitespace()
        {
            while (!IsAtEnd && char.IsWhiteSpace(CurrentChar))
            {
                _position++;
            }
        }

        private bool TryConsume(char expected)
        {
            SkipWhitespace();
            if (!IsAtEnd && CurrentChar == expected)
            {
                _position++;
                return true;
            }
            return false;
        }

        private static double Factorial(double n)
        {
            if (n < 0 || n != Math.Truncate(n))
            {
                throw new FormatException("Factorial is only defined for non-negative integers");
            }
            double result = 1;
            for (int i = 2; i <= (int)n; i++)
            {
                result *= i;
            }
            return result;
        }
    }
}
