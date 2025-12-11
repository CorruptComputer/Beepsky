//using NetCord.Gateway;
//using NetCord.Hosting.Gateway;
//
//namespace Beepsky.DiscordEventHandlers;
//
///// <summary>
/////   Handles the VoiceStateUpdate event from NetCord
///// </summary>
//public class VoiceStateUpdateHandler(GatewayClient client) : IVoiceStateUpdateGatewayHandler
//{
//    /// <inheritdoc />
//    public async ValueTask HandleAsync(VoiceState arg)
//    {
//        // Happened to the bot
//        if (arg.UserId == client.Id)
//        {
//            if (arg.ChannelId is null)
//            {
//                //queue.ClearQueue(arg.GuildId);
//                //playback.StopPlayback(arg.GuildId);
//            }
//        }
//        // Someone else disconnected from voice
//        else if (arg.ChannelId is null)
//        {
//            // If the bot is alone in the voice channel, disconnect and clear the queue
//        }
//    }
//}
