namespace Beepsky.Core.PipelineBehaviors;

/// <inheritdoc />
public class QueryBehavior<TRequest, TValue>
    : PipelineBehaviorBase<TRequest, QueryResponse<TValue>> where TRequest : notnull
{
    /// <inheritdoc />
    protected override bool GetResult(QueryResponse<TValue> response)
    {
        return response.Success;
    }

    /// <inheritdoc />
    protected override QueryResponse<TValue> GetGenericFailedResponse()
    {
        return QueryResponse<TValue>.Fail();
    }
}