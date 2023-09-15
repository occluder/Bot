using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

public class ChatUtils: BotModule
{
    private const int MAX_YEAR_OFFSET = 10;
    private static readonly Dictionary<double, TimeZoneInfo> _timeZones = new();

    private static ValueTask OnMessage(Privmsg message)
    {
        string[] args = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length > 1)
            return default;

        return args[0] switch
        {
            "aest" => message.ReplyWith(Date(10)),
            "acst" => message.ReplyWith(Date(9.5)),
            "awst" => message.ReplyWith(Date(8)),
            "eest" or "ast" => message.ReplyWith(Date(3)),
            "cest" or "eet" => message.ReplyWith(Date(2)),
            "cet" => message.ReplyWith(Date(1)),
            "utc" or "gmt" => message.ReplyWith(Date()),
            "et" or "edt" => message.ReplyWith(Date(-4)),
            "pt" or "pdt" => message.ReplyWith(Date(-7)),
            "pst" => message.ReplyWith(Date(-8)),
            "unix" => message.ReplyWith(DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()),
            { Length: >= 10 and < 13 } unix when long.TryParse(unix, out long time) && WithinReasonableTime(time) =>
                message.ReplyWith(Date(unix: time)),

            { Length: >= 13 } unixMs when long.TryParse(unixMs, out long time) && WithinReasonableTime(time, true) =>
                message.ReplyWith(Date(unix: time, ms: true)),

            { Length: >= 19 } date when DateTimeOffset.TryParse(date, out DateTimeOffset dateTime) => message.ReplyWith(
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

            return $"{offset:yyyy-M-d h:mm:ss tt} [{offset:O}]";
        }

        var utc = DateTimeOffset.UtcNow;
        if (!_timeZones.TryGetValue(hourOffset, out TimeZoneInfo? tz))
        {
            tz = TimeZoneInfo.GetSystemTimeZones().First(t => t.BaseUtcOffset.TotalHours == hourOffset);
            _timeZones.Add(hourOffset, tz);
        }

        var date = TimeZoneInfo.ConvertTime(utc, tz);
        return $"{date:yyyy-M-d h:mm:ss tt (zz)} [{date:O}]";
    }

    private static bool WithinReasonableTime(long time, bool ms = false)
    {
        int year = DateTime.Now.Year;
        var date = ms ? DateTimeOffset.FromUnixTimeMilliseconds(time) : DateTimeOffset.FromUnixTimeSeconds(time);
        return date.Year <= year + MAX_YEAR_OFFSET && date.Year >= year - MAX_YEAR_OFFSET;
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