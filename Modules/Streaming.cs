using Bot.Models;
using MiniTwitch.Helix;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

public class Streaming: BotModule
{
    static readonly ILogger _logger = ForContext<Streaming>();
    static readonly HelixWrapper _client = new(Config.Secrets["ParentToken"], Config.Ids["ParentId"]);
    static readonly long _parentId = Config.Ids["ParentId"];

    async ValueTask OnMessage(Privmsg message)
    {
        if (message.Channel.Id != _parentId || !message.Content.StartsWith("!test"))
        {
            return;
        }

        var res = await _client.GetChannelFollowers(first: 100);
        if (res.Success)
        {
            Console.WriteLine(string.Join('\n', res.Value.Data.Select(x => x.FollowerName)));
        }
    }

    protected override ValueTask OnModuleEnabled()
    {
        MainClient.OnMessage += OnMessage;
        AnonClient.OnMessage += OnMessage;
        return default;
    }
    protected override ValueTask OnModuleDisabled()
    {
        MainClient.OnMessage -= OnMessage;
        AnonClient.OnMessage -= OnMessage;
        return default;
    }
}
