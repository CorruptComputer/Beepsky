using NetCord.Gateway;

namespace Beepsky.Features.Chatty;

/// <inheritdoc />
public sealed class BeepskyChat : IRequestHandler<BeepskyChat.Command>
{
    /// <summary>
    ///   Handles a direct mention of Beepsky
    /// </summary>
    /// <param name="Message"></param>
    public record Command(Message Message) : IRequest;

    /// <inheritdoc />
    public async Task Handle(Command request, CancellationToken cancellationToken)
    {
        // TODO: I'd like to have some really tiny self-hosted LLM to generate simple responses for these.
        //       Ideally, something small enough to run on a Raspberry Pi. But, problem for future me.
        if (request.Message.Author.Id is (ulong)WellKnownUsers.Skeleton
                      or (ulong)WellKnownUsers.Saeryn
                      or (ulong)WellKnownUsers.Svally)
        {
            await request.Message.ReplyAsync("STOP RIGHT THERE YOU CRIMINAL SCUM", cancellationToken: cancellationToken);
        }
        else
        {
            await request.Message.ReplyAsync("HAVE A SECURE DAY", cancellationToken: cancellationToken);
        }
    }
}