using Beepsky.Core.Exceptions;
using Beepsky.Core.Models.MathExpressions;
using Serilog;

namespace Beepsky.Core.Features.TerribleCounting;

/// <inheritdoc />
public sealed class GenerateExpressionTree : IRequestHandler<GenerateExpressionTree.Query, QueryResponse<Expression>>
{
    /// <summary>
    ///   Generates an expression tree that evaluates to the target value.
    /// </summary>
    /// <param name="TargetValue">The value the expression should evaluate to.</param>
    public record Query(int TargetValue) : IRequest<QueryResponse<Expression>>;

    /// <inheritdoc />
    public Task<QueryResponse<Expression>> Handle(Query request, CancellationToken cancellationToken)
    {
        int operations = GenerationContext.GetOperationsForTarget(request.TargetValue);
        GenerationContext ctx = new(operations);

        ExpressionNode root = GenerateNode(request.TargetValue, ctx);
        Expression expression = new(root);

        return Task.FromResult<QueryResponse<Expression>>(expression);
    }

    private sealed class GenerationContext(int remainingOperations, OperatorKind? lastOperator = null)
    {
        public const int MaxOperations = 4;
        public const int MaxFactorialInput = 8;
        public const int MaxExponent = 6;

        public int RemainingOperations { get; private set; } = remainingOperations;
        public OperatorKind? LastOperator { get; private set; } = lastOperator;

        /// <summary>
        /// Attempts to consume an operation. Returns false if no operations remain.
        /// </summary>
        public bool TryUseOperation(OperatorKind op)
        {
            if (RemainingOperations <= 0)
                return false;

            RemainingOperations--;
            LastOperator = op;
            return true;
        }

        /// <summary>
        /// Calculates the number of operations based on the target value.
        /// Scales logarithmically: small numbers get fewer operations, larger numbers get more.
        /// </summary>
        public static int GetOperationsForTarget(int target)
        {
            // Use log10 of absolute value to scale complexity
            // 1-9: 0 ops, 10-99: 1 op, 100-999: 2 ops, 1000-9999: 3 ops, 10000+: 4 ops
            int absTarget = Math.Max(1, Math.Abs(target));
            int operations = (int)Math.Log10(absTarget);
            return Math.Clamp(operations, 0, MaxOperations);
        }
    }

    private sealed record InverseCandidate(OperatorKind Operator, int LeftValue, int? RightValue);

    private ExpressionNode GenerateNode(int target, GenerationContext ctx)
    {
        // Base case: no operations remaining, emit literal
        if (ctx.RemainingOperations <= 0)
        {
            return new ValueNode(target);
        }

        List<InverseCandidate> candidates = GenerateInverseCandidates(target);

        // Randomize order, biasing against repeating the same operator
        WeightedShuffle(candidates, ctx.LastOperator);

        foreach (InverseCandidate candidate in candidates)
        {
            // Check if we can use an operation before committing
            if (!ctx.TryUseOperation(candidate.Operator))
            {
                return new ValueNode(target);
            }

            try
            {
                return BuildNode(candidate, ctx);
            }
            catch (Exception e)
            {
                // Failed path, try another candidate
                Log.Error(e, "Failed to build node for target {Target} with candidate {Candidate}", target, candidate);
            }
        }

        // Fallback: literal
        return new ValueNode(target);
    }

    private static List<InverseCandidate> GenerateInverseCandidates(int target)
    {
        List<InverseCandidate> list = new List<InverseCandidate>();

        // Absolute value
        if (target != 0)
        {
            list.Add(new InverseCandidate(
                OperatorKind.AbsoluteValue,
                -target,
                null));
        }

        // Factorial (precomputed check)
        for (int i = 0; i <= GenerationContext.MaxFactorialInput; i++)
        {
            if (Factorial(i) == target)
            {
                list.Add(new InverseCandidate(
                    OperatorKind.Factorial,
                    i,
                    null));
            }
        }

        // Multiplication (factor pairs)
        for (int i = 2; i <= Math.Sqrt(target); i++)
        {
            if (target % i == 0)
            {
                list.Add(new InverseCandidate(
                    OperatorKind.Multiply,
                    i,
                    target / i));
            }
        }

        // Power (integer roots)
        for (int exp = 2; exp <= GenerationContext.MaxExponent; exp++)
        {
            double root = Math.Round(Math.Pow(target, 1.0 / exp));
            int intRoot = (int)root;
            if (intRoot > 0 && (int)Math.Pow(intRoot, exp) == target)
            {
                list.Add(new InverseCandidate(
                    OperatorKind.Power,
                    intRoot,
                    exp));
            }
        }

        // Addition / subtraction
        int offset = Random.Shared.Next(1, 10);
        list.Add(new InverseCandidate(
            OperatorKind.Add,
            target - offset,
            offset));

        list.Add(new InverseCandidate(
            OperatorKind.Subtract,
            target + offset,
            offset));

        // Division (only exact divisions, work backwards: dividend / divisor = target)
        for (int divisor = 2; divisor <= 10; divisor++)
        {
            int dividend = target * divisor;
            list.Add(new InverseCandidate(
                OperatorKind.Divide,
                dividend,
                divisor));
        }

        return list;
    }

    private OperationNode BuildNode(InverseCandidate c, GenerationContext ctx)
    {
        return c.Operator switch
        {
            OperatorKind.AbsoluteValue =>
                new(OperatorKind.AbsoluteValue,
                    GenerateNode(c.LeftValue, ctx),
                    null,
                    Math.Abs(c.LeftValue)),

            OperatorKind.Factorial =>
                new(OperatorKind.Factorial,
                    GenerateNode(c.LeftValue, ctx),
                    null,
                    Factorial(c.LeftValue)),

            OperatorKind.Power =>
                new(OperatorKind.Power,
                    GenerateNode(c.LeftValue, ctx),
                    new ValueNode(c.RightValue!.Value),
                    (int)Math.Pow(c.LeftValue, c.RightValue.Value)),

            OperatorKind.Multiply =>
                new(OperatorKind.Multiply,
                    GenerateNode(c.LeftValue, ctx),
                    new ValueNode(c.RightValue!.Value),
                    c.LeftValue * c.RightValue.Value),

            OperatorKind.Add =>
                new(OperatorKind.Add,
                    GenerateNode(c.LeftValue, ctx),
                    new ValueNode(c.RightValue!.Value),
                    c.LeftValue + c.RightValue.Value),

            OperatorKind.Subtract =>
                new(OperatorKind.Subtract,
                    GenerateNode(c.LeftValue, ctx),
                    new ValueNode(c.RightValue!.Value),
                    c.LeftValue - c.RightValue.Value),

            OperatorKind.Divide =>
                new(OperatorKind.Divide,
                    GenerateNode(c.LeftValue, ctx),
                    new ValueNode(c.RightValue!.Value),
                    c.LeftValue / c.RightValue.Value),

            _ => throw new BeepskyException("Unsupported operator in inverse candidate.")
        };
    }

    private static int Factorial(int n)
    {
        int result = 1;
        for (int i = 2; i <= n; i++)
            result *= i;
        return result;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static void WeightedShuffle(List<InverseCandidate> candidates, OperatorKind? lastOperator)
    {
        // Assign weights: lower weight for the last-used operator
        const double repeatPenalty = 0.1; // 10% chance compared to others

        List<(InverseCandidate Candidate, double Weight)> weighted = [.. candidates
            .Select(c => (
                Candidate: c,
                Weight: c.Operator == lastOperator ? repeatPenalty : 1.0
            ))];

        // Weighted shuffle using reservoir-like sampling
        candidates.Clear();
        while (weighted.Count > 0)
        {
            double totalWeight = weighted.Sum(w => w.Weight);
            double pick = Random.Shared.NextDouble() * totalWeight;

            double cumulative = 0;
            int selectedIndex = 0;
            for (int i = 0; i < weighted.Count; i++)
            {
                cumulative += weighted[i].Weight;
                if (pick <= cumulative)
                {
                    selectedIndex = i;
                    break;
                }
            }

            candidates.Add(weighted[selectedIndex].Candidate);
            weighted.RemoveAt(selectedIndex);
        }
    }
}
