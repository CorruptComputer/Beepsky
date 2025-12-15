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
        "https://www.youtube.com/watch?v=pDGl2albRHk", // Wii Shop - All I Want for Christmas is You
        "https://www.youtube.com/watch?v=27sMIlSMqFE", // Bing Crosby - Mele Kalikimaka
        "https://www.youtube.com/watch?v=6JeAnrziLLo", // jschlatt - Santa Claus is Coming to Town
        "https://www.youtube.com/watch?v=1Ggr94QhXo4", // jschlatt - The Christmas Song
        "https://www.youtube.com/watch?v=5O4yLDVBr4s", // jschlatt - Let it snow
        "https://www.youtube.com/watch?v=KR7eJft_aqc", // jschlatt - Baby It's Cold Outside
        "https://www.youtube.com/watch?v=dZVT60baWWA", // jschlatt - Happy Holidays
        "https://www.youtube.com/watch?v=jZ0Q5zVCVJ8", // jschlatt - White Christmas
        "https://www.youtube.com/watch?v=nyoQ_5Q7geo", // jschlatt - It's the Most Wonderful Time of the Year
        "https://www.youtube.com/watch?v=uTJ4FXbzVBA", // jschlatt - Have Yourself a Merry Little Christmas
        "https://www.youtube.com/watch?v=N6YE6ocl27o", // jschlatt - Mele Kalikimaka
        "https://www.youtube.com/watch?v=f4p2-bp4zmc", // jschlatt - Sleigh Ride
        "https://www.youtube.com/watch?v=ziCsclD9jnY", // jschlatt - The Man with the Bag
        "https://www.youtube.com/watch?v=fEbFfoBPfw4", // Snow Miser - I'm Mr. White Christmas
        "https://www.youtube.com/watch?v=7T4uI9Kde4U", // Heat Miser - I'm Mr. Green Christmas
    ];

    /// <summary>
    ///   Queues 5 random christmas songs for playback in the channel
    /// </summary>
    /// <param name="GuildId"></param>
    /// <param name="VoiceChannelId"></param>
    public record Command(ulong GuildId, ulong VoiceChannelId) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
    {
        // Get the songs in a random order
        IEnumerable<string> shuffledSongs = christmasSongs.OrderBy(_ => rdm.Next()).Take(5);

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
