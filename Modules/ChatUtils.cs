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
            "eest" or "ast" => message.ReplyWith(Date(3)),
            "cest" or "eet" => message.ReplyWith(Date(2)),
            "cet" => message.ReplyWith(Date(1)),
            "utc" or "gmt" => message.ReplyWith(Date()),
            "et" or "edt" => message.ReplyWith(Date(-4)),
            "pt" or "pdt" => message.ReplyWith(Date(-7)),
            "unix" => message.ReplyWith(DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()),
            { Length: >= 10 and < 13 } unix when long.TryParse(unix, out long time) && WithinReasonableTime(time) =>
                message.ReplyWith(Date(unix: time)),

            { Length: >= 13 } unixMs when long.TryParse(unixMs, out long time) && WithinReasonableTime(time, true) =>
                message.ReplyWith(Date(unix: time, ms: true)),

            { Length: >= 28 } date when DateTimeOffset.TryParse(date, out var dateTime) => message.ReplyWith(
                dateTime.ToUnixTimeMilliseconds().ToString()),

            _ => default
        };
    }

    private static string Date(double hourOffset = 0, long? unix = null, bool ms = false)
    {
        if (unix is not null)
        {
            var offset = ms
                ? DateTimeOffset.FromUnixTimeMilliseconds(unix.Value)
                : DateTimeOffset.FromUnixTimeSeconds(unix.Value);
            return $"{offset:yyyy-M-d} {offset:h:mm:ss tt} [{offset:O}]";
        }

        var utc = DateTimeOffset.UtcNow;
        TimeZoneInfo tz = TimeZoneInfo.GetSystemTimeZones().First(t => t.BaseUtcOffset.TotalHours == hourOffset);
        var date = TimeZoneInfo.ConvertTime(utc, tz);
        return $"{date:yyyy-M-d} {date:h:mm:ss tt zz} [{date:O}]";
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