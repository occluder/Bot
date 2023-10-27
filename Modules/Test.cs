using Bot.Models;
using MiniTwitch.Helix.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

public class Test: BotModule
{
    protected override ValueTask OnModuleEnabled()
    {
        AnonClient.OnMessage += OnMessage;
        return default;
    }

    protected override ValueTask OnModuleDisabled()
    {
        AnonClient.OnMessage -= OnMessage;
        return default;
    }

    private static async ValueTask OnMessage(Privmsg arg)
    {
        if (Random.Shared.Next(2000) != 10)
            return;

        HelixResult result = await HelixClient.SendChatAnnouncement(Config.Ids["ParentId"], Config.Ids["BotId"], new()
        {
            Message = arg.Content + arg.GetHashCode()
        });

        Information("{@BanResult}", result);
    }
}