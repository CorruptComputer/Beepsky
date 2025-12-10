namespace Beepsky.PipelineBehaviors;

/// <inheritdoc />
public class CommandBehavior<TRequest>
    : PipelineBehaviorBase<TRequest, CommandResponse> where TRequest : notnull
{
    /// <inheritdoc />
    protected override bool GetResult(CommandResponse response)
    {
        return response.Success;
    }

    /// <inheritdoc />
    protected override CommandResponse GetGenericFailedResponse()
    {
        return CommandResponse.Fail();
    }
}