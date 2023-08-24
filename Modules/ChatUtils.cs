using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

public class ChatUtils: BotModule
{
    private static ValueTask OnMessage(Privmsg message)
    {
        if (message.Channel.Id is not 11148817 and not 780092850)
            return default;

        string[] args = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length > 1)
            return default;

        return args[0] switch
        {
            "utc" => message.ReplyWith(DateTime.UtcNow.ToString("O")),
            "unix" => message.ReplyWith(DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()),
            { Length: >= 10 and < 13 } unix when long.TryParse(unix, out long time) && WithinReasonableTime(time) =>
                message.ReplyWith(DateTimeOffset.FromUnixTimeSeconds(time).ToString("O")),

            { Length: >= 13 } unixMs when long.TryParse(unixMs, out long time) && WithinReasonableTime(time, true) =>
                message.ReplyWith(DateTimeOffset.FromUnixTimeMilliseconds(time).ToString("O")),

            { Length: >= 28 } date when DateTimeOffset.TryParse(date, out var dateTime) => message.ReplyWith(
                dateTime.ToUnixTimeMilliseconds().ToString()),

            _ => default
        };
    }

    private static bool WithinReasonableTime(long time, bool ms = false)
    {
        int year = DateTime.Now.Year;
        var date = ms ? DateTimeOffset.FromUnixTimeMilliseconds(time) : DateTimeOffset.FromUnixTimeSeconds(time);
        return date.Year <= year + 5 && date.Year >= year - 5;
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