using Beepsky.Features.Chatty.Normal;
using NetCord.Gateway;

namespace Beepsky.Features.Chatty;

/// <inheritdoc />
public sealed class NormalChat(ISender sender) : IRequestHandler<NormalChat.Command>
{
    /// <summary>
    ///   Handles a normal chat message, not a command or direct mention
    /// </summary>
    /// <param name="Message"></param>
    public record Command(Message Message) : IRequest;

    /// <inheritdoc />
    public async Task Handle(Command request, CancellationToken cancellationToken)
    {
        if (request.Message.Author.Id == (ulong)WellKnownUsers.Skeleton)
        {
            // First funny, tell skeleton to go back to the warhammer channel if he talks about it outside there, 1 in 20 chance
            // Roll a d20
            int roll = rdm.Next(20) + 1;

            // Oh yeah, its show time
            if (roll == 20)
            {
                bool isWarhammerRelated = await sender.Send(new IsMessageWarhammerRelated.Command(request.Message.Content), cancellationToken);

                if (isWarhammerRelated)
                {
                    await sender.Send(new TellSkeletonHeSmells.Command(request.Message), cancellationToken);
                    return;
                }
            }

            // Possible second funny, 1 in 1,000,000 to respond to any message of his with something
        }
        else if (request.Message.Author.Id == (ulong)WellKnownUsers.Monke)
        {
            if (request.Message.Content.Contains("banana", StringComparison.OrdinalIgnoreCase))
            {
                await request.Message.ReplyAsync("🍌", cancellationToken: cancellationToken);
            }
        }
    }

    private static readonly Random rdm = new();
}