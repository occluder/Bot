using Bot.Models;
using MiniTwitch.Helix.Models;
using MiniTwitch.Helix.Responses;
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
        if (Random.Shared.Next(1000) != 10)
            return;

        HelixResult<BannedUser> result = await HelixClient.BanUser(Config.Ids["ParentId"], Config.Ids["BotId"], new()
        {
            Data = new()
            {
                UserId = arg.Author.Id,
                Duration = TimeSpan.FromSeconds(Random.Shared.Next(1000)),
                Reason = $"xD {arg.GetHashCode()}"
            }
        });

        Information("{@BanResult}", result);
    }
}