using Beepsky.Core.Database.DbSets;
using Beepsky.Core.Database.Operations.Guild;
using Beepsky.Core.Database.Operations.GuildUserStatistic;
using Beepsky.Core.Database.Operations.User;

namespace Beepsky.Core.Features.Statistics;

// TODO: This entire class needs to be removed, refactor into smaller operations.

/// <inheritdoc />
public sealed class UpdateUserMessageCount(ISender sender) : IRequestHandler<UpdateUserMessageCount.Command>
{
    /// <summary>
    ///   Update user message count
    /// </summary>
    /// <param name="UserName"></param>
    /// <param name="UserId"></param>
    /// <param name="GuildName"></param>
    /// <param name="GuildId"></param>
    /// <param name="BeepskyCommand"></param>
    /// <param name="BeepskyChat"></param>
    public record Command(string UserName, ulong UserId, string GuildName, ulong GuildId, bool BeepskyCommand, bool BeepskyChat) : IRequest;

    /// <inheritdoc />
    public async Task Handle(Command request, CancellationToken cancellationToken)
    {
        DiscordGuild? guild = await sender.Send(new GetGuildDb.Command(request.GuildId), cancellationToken);
        if (guild is null)
        {
            guild = new DiscordGuild
            {
                GuildId = request.GuildId,
                Name = request.GuildName ?? throw new InvalidOperationException("Guild name is null"),
            };

            await sender.Send(new CreateGuildDb.Command(guild), cancellationToken);
        }

        DiscordUser? user = await sender.Send(new GetUserDb.Command(request.UserId), cancellationToken);
        if (user is null)
        {
            user = new DiscordUser
            {
                UserId = request.UserId,
                Username = request.UserName ?? throw new InvalidOperationException("User username is null"),
                Mention = $"<@{request.UserId}>",
            };

            await sender.Send(new CreateUserDb.Command(user), cancellationToken);
        }

        DiscordGuildUserStatistic? guildUserStatistic = await sender.Send(new GetGuildUserStatistic.Command(request.GuildId, request.UserId, (ushort)DateTime.UtcNow.Year), cancellationToken);
        if (guildUserStatistic is null)
        {
            guildUserStatistic = new DiscordGuildUserStatistic
            {
                GuildId = request.GuildId,
                UserId = request.UserId,
                Year = (ushort)DateTimeOffset.UtcNow.Year,
                MessagesSent = 1,
                BeepskyCommandsUsed = request.BeepskyCommand
                    ? 1
                    : 0,
                BeepskyChattedWith = request.BeepskyChat
                    ? 1
                    : 0,
                MostChattyDay = DateOnly.FromDateTime(DateTime.UtcNow),
                MessagesSentOnMostChattyDay = 1,
                Today = DateOnly.FromDateTime(DateTime.UtcNow),
                MessagesSentToday = 1,
                TimesJoinedVoice = 0,
                TimesKickedFromVoice = 0,
                MessagesDeletedFrom = 0,
                MessagesDeletedBy = 0
            };

            await sender.Send(new CreateGuildUserStatistic.Command(guildUserStatistic), cancellationToken);
        }
        else
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

            guildUserStatistic.MessagesSent += 1;
            if (guildUserStatistic.MostChattyDay == today)
            {
                guildUserStatistic.MessagesSentOnMostChattyDay += 1;
            }
            else if (guildUserStatistic.MessagesSentToday + 1 > guildUserStatistic.MessagesSentOnMostChattyDay)
            {
                guildUserStatistic.MostChattyDay = today;
                guildUserStatistic.MessagesSentOnMostChattyDay = guildUserStatistic.MessagesSentToday + 1;
            }

            if (guildUserStatistic.Today == today)
            {
                guildUserStatistic.MessagesSentToday += 1;
            }
            else
            {
                guildUserStatistic.Today = today;
                guildUserStatistic.MessagesSentToday = 1;
            }

            if (request.BeepskyCommand)
            {
                guildUserStatistic.BeepskyCommandsUsed += 1;
            }

            if (request.BeepskyChat)
            {
                guildUserStatistic.BeepskyChattedWith += 1;
            }

            await sender.Send(new UpdateGuildUserStatistic.Command(guildUserStatistic), cancellationToken);
        }
    }
}
