using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Services.Commands;
using NetCord.Services;
using NetCord.Services.Commands;

namespace Beepsky.Features.Commands;

/// <summary>
///   Handles command execution results for Beepsky, loosely based on NetCord's default CommandResultHandler, but with the "Command not found" response removed:
///   https://github.com/NetCordDev/NetCord/blob/alpha/Hosting/NetCord.Hosting.Services/Commands/CommandResultHandler.cs
/// </summary>
/// <typeparam name="TContext"></typeparam>
/// <param name="messageFlags"></param>
public class BeepskyCommandResultHandler<TContext>(MessageFlags? messageFlags = null) : ICommandResultHandler<TContext>
    where TContext : ICommandContext
{
    /// <inheritdoc />
    public ValueTask HandleResultAsync(IExecutionResult result, TContext context, GatewayClient client, ILogger logger, IServiceProvider services)
    {
        if (result is IFailResult failResult
            // Exclude NotFoundResult to avoid "Command not found" messages
            && failResult is not NotFoundResult)
        {
            string resultMessage = failResult.Message;
            Message message = context.Message;

            return new(message.ReplyAsync(new()
            {
                Content = resultMessage,
                FailIfNotExists = false,
                Flags = messageFlags,
            }));
        }

        return default;
    }
}