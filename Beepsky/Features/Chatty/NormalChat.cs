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
        if (request.Message.Author.Id is (ulong)WellKnownUsers.Skeleton
        // These kinda suck to read, but too bad! I need a way to test this
#if DEBUG
         or (ulong)WellKnownUsers.Monke)
#else
         )
#endif
        {
            // Tell skeleton to go back to the warhammer channel if he talks about it outside there, 1 in 20 chance
            bool isWarhammerRelated = await sender.Send(new IsMessageWarhammerRelated.Command(request.Message.Content), cancellationToken);

            if (isWarhammerRelated
                && request.Message.ChannelId != (ulong)WellKnownChannels.Warhammer)
            {
                // Roll a d20
                int roll =
#if DEBUG
                20;
#else
                rdm.Next(20) + 1;
#endif
                // Oh yeah, its show time
                if (roll == 20)
                {
                    await sender.Send(new TellSkeletonHeSmells.Command(request.Message), cancellationToken);
                    return;
                }
            }

            // 1 in 1,000,000 to respond to any message of his with something?
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