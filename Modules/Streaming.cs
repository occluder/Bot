using Bot.Models;
using MiniTwitch.Helix;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

public class Streaming: BotModule
{
    static readonly ILogger _logger = ForContext<Streaming>();
    static readonly HelixWrapper _client = new(Config.Secrets["ParentToken"], Config.Ids["ParentId"]);
    static readonly long _parentId = Config.Ids["ParentId"];
    Dictionary<string, long> _followers = [];

    async ValueTask OnMessage(Privmsg message)
    {
        if (message.Channel.Id != _parentId || !message.Content.StartsWith("!deadlock"))
        {
            return;
        }

        var res = await _client.GetChannelFollowers(first: 100);
        if (!res.Success)
        {
            return;
        }

        _followers.Clear();
        _followers = res.Value.Data.ToDictionary(
            x => x.FollowerName,
            x => Unix() - new DateTimeOffset(x.FollowedAt).ToLocalTime().ToUnixTimeSeconds()
        );

        if (
            !_followers.TryGetValue(message.Author.Name, out var followage)
            || TimeSpan.FromMinutes(30) > TimeSpan.FromSeconds(followage)
        )
        {
            await message.ReplyWith(
                "Want to get invited to the closed beta of Valve's new game, Deadlock? " +
                $"Follow the stream and watch for 30 minutes, then use this command again. ({TimeSpan.FromSeconds(followage):m'm's's left'})"
            );

            return;
        }

        await message.ReplyWith("Send your Steam friend code, I will add you as a friend and invite you to the closed beta");
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
