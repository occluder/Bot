using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

public class ChatUtils: BotModule
{
    private static ValueTask OnMessage(Privmsg message)
    {
        if (message.Channel.Id != 11148817)
            return default;

        string[] args = message.Content.Split(' ');
        return args[0] switch
        {
            "utc" => message.ReplyWith(DateTime.UtcNow.ToString("O")),
            "unix" => message.ReplyWith(DateTimeOffset.Now.ToUnixTimeSeconds().ToString()),
            { Length: >= 10 and < 13 } unix when long.TryParse(unix, out long time) => message.ReplyWith(DateTimeOffset.FromUnixTimeSeconds(time).ToString()),
            { Length: >= 13 } unixMs when long.TryParse(unixMs, out long time) => message.ReplyWith(DateTimeOffset.FromUnixTimeMilliseconds(time).ToString()),
            { Length: >= 28 } date when DateTime.TryParse(date, out DateTime dateTime) => message.ReplyWith(dateTime.ToString("O")),
            _ => default
        };
    }

    protected override ValueTask OnModuleEnabled()
    {
        MainClient.OnMessage += OnMessage;
        return default;
    }

    protected override ValueTask OnModuleDisabled()
    {
        MainClient.OnMessage -= OnMessage;
        return default;
    }
}