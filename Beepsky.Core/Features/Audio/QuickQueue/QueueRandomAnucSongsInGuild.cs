using Beepsky.Core.Extensions;

namespace Beepsky.Core.Features.Audio.QuickQueue;

/// <inheritdoc />
public sealed class QueueRandomAnucSongsInGuild(ISender sender)
    : IRequestHandler<QueueRandomAnucSongsInGuild.Command, CommandResponse>
{
    private static readonly string[] anucSongs =
    [
        "https://www.youtube.com/watch?v=JFKrq4MSzIE", // Just the two of us. (2022)
        "https://www.youtube.com/watch?v=ezQI3LNWkM0", // I just call to say I love you. Cover (2023)
        "https://www.youtube.com/watch?v=icpW-G1zq3M", // The Phanthom of the Opera. cover (2023)
        "https://www.youtube.com/watch?v=3Hizr32jtZM", // Country roads, Take me home. Cover (2023)
        "https://www.youtube.com/watch?v=ABSB9zedyWQ", // What a wonderful world. cover. (2023)
        "https://www.youtube.com/watch?v=qGL4foSDAgI", // Barbie girl. Cover. (2023)
        "https://www.youtube.com/watch?v=2Cug3XeoMk4", // Can't help falling in love. cover (2023)
        "https://www.youtube.com/watch?v=_t_AAy5Lgeg", // Snake Eater. Cover. (2023)
        "https://www.youtube.com/watch?v=blbxIwJVUN0", // One piece, We are. (2023)
        "https://www.youtube.com/watch?v=xl4mZsk6kVw", // Fighting gold. Jojo bizarre adventure. (2023)
        "https://www.youtube.com/watch?v=gwGpJrVcNFU", // One piece. Believe in wonderland. Cover (2023)
        "https://www.youtube.com/watch?v=sClc_OImYE0", // Hallelujah. Cover (2023)
        "https://www.youtube.com/watch?v=veu0z8ZBQt4", // I'm just Ken. Cover. (2023)
        "https://www.youtube.com/watch?v=BYqhOXQaKLw", // Sasageyo. Attack on Titan. Cover. (2023)
        "https://www.youtube.com/watch?v=feEUBdH8k5U", // We don't talk anymore. Cover (2023)
        "https://www.youtube.com/watch?v=F4zpfvJnxK0", // Diamonds. Cover (2023)
        "https://www.youtube.com/watch?v=b5FG01QQwK8", // One piece. Jungle P. (2023)
        "https://www.youtube.com/watch?v=uoH66iAh9e8", // Thriller. Cover (2023)
        "https://www.youtube.com/watch?v=WxV5KBXGsK4", // Bloody stream. Jojo bizarre adventure. Cover. (2024)
        "https://www.youtube.com/watch?v=ysaKLKC5FtY", // Baka mitai. Cover. (2024)
        "https://www.youtube.com/watch?v=j3-TDQNwlTM", // Unravel, Tokyo ghoul. cover (2024)
        "https://www.youtube.com/watch?v=tS9ZnIwIhuo", // Akuma no ko. Attack on titan Ost. Cover (2024)
        "https://www.youtube.com/watch?v=QjjeaF_wq98", // Careless whisper. Cover. (2024)
        "https://www.youtube.com/watch?v=QY9tnJ0XlC4", // Fly me to the moon. Cover. (2024)
        "https://www.youtube.com/watch?v=iwT7Ii2c7oA", // Bring me to life. Cover (2024)
        "https://www.youtube.com/watch?v=Yw3QHGxonGU", // Creep. Cover (2024)
        "https://www.youtube.com/watch?v=s8dc33X16PI", // Cherri Cherri lady. Cover (2025)
        "https://www.youtube.com/watch?v=ZqiVKAEk0hQ", // Cruel Angel’s thesis. Cover. (2025)
        "https://www.youtube.com/watch?v=thLzxCeMdmo", // Your man. Cover. (2025)
        "https://www.youtube.com/watch?v=FI-kS4MuYds", // Die with a smile. Cover. (2025)
        "https://www.youtube.com/watch?v=5KTcebBWDXo", // It's might be you. Cover. (2025)
        "https://www.youtube.com/watch?v=oiDjqTUUhGQ", // Heal the world. Cover. (2025)
        "https://www.youtube.com/watch?v=EM25PDhhzRo", // Crazy Noisy Bizarre Town. Cover (2025)
        "https://www.youtube.com/watch?v=q87-7jsOCnA", // We are, One piece. Cover (2025)
    ];

    /// <summary>
    ///   Queues 5 random Anuc songs for playback in the channel
    /// </summary>
    /// <param name="GuildId"></param>
    /// <param name="VoiceChannelId"></param>
    public record Command(ulong GuildId, ulong VoiceChannelId) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
    {
        // Get 5 random songs
        IEnumerable<string> shuffledSongs = Random.Shared.GetUniqueItems(anucSongs, 5);

        foreach (string song in shuffledSongs)
        {
            bool success = await sender.Send(new QueueTrackInGuild.Command(request.GuildId, request.VoiceChannelId, song), cancellationToken);
            if (!success)
            {
                return CommandResponse.Fail("Failed to add a track to the queue: " + song);
            }
        }

        return CommandResponse.Pass();
    }
}
