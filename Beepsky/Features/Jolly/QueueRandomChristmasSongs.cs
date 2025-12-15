using Beepsky.Database.DbSets;
using Beepsky.Database.Operations.Guild;
using Beepsky.Database.Operations.GuildUserStatistic;
using Beepsky.Database.Operations.User;
using Beepsky.Services;
using NetCord.Gateway;

namespace Beepsky.Features.Jolly;

/// <inheritdoc />
public sealed class QueueRandomChristmasSongs(AudioQueueService audioQueueService) : IRequestHandler<QueueRandomChristmasSongs.Command, CommandResponse>
{
    private static readonly Random rdm = new();
    private static readonly List<string> christmasSongs =
    [
        "https://www.youtube.com/watch?v=YfF10ow4YEo", // Kelly Clarkson - Underneath the Tree
        "https://www.youtube.com/watch?v=KhqNTjbQ71A", // Wham! - Last Christmas
        "https://www.youtube.com/watch?v=7LD9LfW8m4M", // Michael Bublé - Holly Jolly Christmas
        "https://www.youtube.com/watch?v=RmUWWVZw28E", // Mariah Carey - All I Want For Christmas Is You
        "https://www.youtube.com/watch?v=Rnil5LyK_B0", // Dean Martin - Let it snow
        "https://www.youtube.com/watch?v=1qYz7rfgLWE", // Brenda Lee - Rockin' Around the Christmas Tree
        "https://www.youtube.com/watch?v=cRbLtqbPYjU", // José Feliciano - Feliz Navidad
        "https://www.youtube.com/watch?v=nIhs1T7OcZg", // Bobby Helms - Jingle Bell Rock
        "https://www.youtube.com/watch?v=hLf0-lro8X8", // Frank Sinatra - Jingle Bells
        "https://www.youtube.com/watch?v=sE3uRRFVsmc", // Frank Sinatra - Let it snow
        "https://www.youtube.com/watch?v=8Q94C9FRRpM", // Frank Sinatra - Santa Claus is Coming to Town
        //"https://www.youtube.com/watch?v=8CKEcOBCSQg", // Tyler, The Creator - I Am The Grinch
        //"https://www.youtube.com/watch?v=oMCG-fL-uCM", // Tyler, The Creator - Big Bag
        //"https://www.youtube.com/watch?v=QKiCGSieghA", // Tyler, The Creator - LIGHTS ON
        "https://www.youtube.com/watch?v=pDGl2albRHk", // Wii Shop - All I Want for Christmas is You
    ];

    /// <summary>
    ///   Queues a random christmas songs for playback in the channel
    /// </summary>
    /// <param name="GuildId"></param>
    /// <param name="VoiceChannelId"></param>
    public record Command(ulong GuildId, ulong VoiceChannelId) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
    {
        // Get the songs in a random order
        IEnumerable<string> shuffledSongs = christmasSongs.OrderBy(_ => rdm.Next());

        foreach (string song in shuffledSongs)
        {
            bool success = audioQueueService.AddTrackToQueue(request.VoiceChannelId, request.GuildId, song);
            if (!success)
            {
                return Task.FromResult(CommandResponse.Fail("Failed to add a track to the queue: " + song));
            }
        }

        return Task.FromResult(CommandResponse.Pass());
    }
}
