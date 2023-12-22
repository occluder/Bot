using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

public class ChatUtils: BotModule
{
    private const int MAX_YEAR_OFFSET = 10;
    private static readonly Dictionary<double, TimeZoneInfo> _timeZones = new();

    private static ValueTask OnMessage(Privmsg message)
    {
        if (UserBlacklisted(message.Author.Id))
            return default;

        ReadOnlySpan<char> m = message.Content;
        int space = m.IndexOf(' ');
        if ((space != -1 && m.Length > space + 1) || space > 4)
            return default;

        try
        {
            return m[..(space == -1 ? ^0 : space)] switch
            {
                "aedt" => message.ReplyWith(Date(660)),
                "acdt" => message.ReplyWith(Date(630)),
                "aest" => message.ReplyWith(Date(600)),
                "acst" => message.ReplyWith(Date(570)),
                "awst" => message.ReplyWith(Date(480)),
                "eest" or "ast" => message.ReplyWith(Date(180)),
                "cest" or "eet" => message.ReplyWith(Date(120)),
                "cet" => message.ReplyWith(Date(60)),
                "utc" or "gmt" => message.ReplyWith(Date()),
                "et" or "edt" => message.ReplyWith(Date(-240)),
                "pt" or "pdt" => message.ReplyWith(Date(-420)),
                "pst" => message.ReplyWith(Date(-480)),
                "unix" => message.ReplyWith(UnixMs().ToString()),

                { Length: 10 } unix when long.TryParse(unix, out long time) && WithinReasonableTime(time) =>
                    message.ReplyWith(Date(unix: time)),

                { Length: 13 } unixMs when long.TryParse(unixMs, out long time) && WithinReasonableTime(time, true) =>
                    message.ReplyWith(Date(unix: time, ms: true)),

                { Length: >= 19 } date when DateTimeOffset.TryParse(date, out DateTimeOffset dateTime) => message
                    .ReplyWith(
                        dateTime.ToUnixTimeMilliseconds().ToString()),

                _ => default
            };
        }
        catch (Exception e)
        {
            ForContext<ChatUtils>().Warning(e, "Parsing exception: ");
            return ValueTask.CompletedTask;
        }
    }

    private static string Date(double minOffset = 0, long? unix = null, bool ms = false)
    {
        if (unix is not null)
        {
            var offset = ms
                ? DateTimeOffset.FromUnixTimeMilliseconds(unix.Value)
                : DateTimeOffset.FromUnixTimeSeconds(unix.Value);

            return $"{offset:yyyy-MM-dd, hh:mm:ss tt} {ShortDistance(DateTimeOffset.Now - offset)}";
        }

        var utc = DateTimeOffset.UtcNow;
        if (!_timeZones.TryGetValue(minOffset, out TimeZoneInfo? tz))
        {
            tz = TimeZoneInfo.GetSystemTimeZones().First(t => (int)t.BaseUtcOffset.TotalMinutes == (int)minOffset);
            _timeZones.Add(minOffset, tz);
        }

        var date = TimeZoneInfo.ConvertTime(utc, tz);
        return $"{date:yyyy-MM-dd, hh:mm:ss tt, (UTCzzz)}";
    }

    private static bool WithinReasonableTime(long time, bool ms = false)
    {
        int year = DateTime.Now.Year;
        var date = ms ? DateTimeOffset.FromUnixTimeMilliseconds(time) : DateTimeOffset.FromUnixTimeSeconds(time);
        return date.Year <= year + MAX_YEAR_OFFSET && date.Year >= year - MAX_YEAR_OFFSET;
    }

    private static string? ShortDistance(TimeSpan distance) => distance switch
    {
        { TotalMinutes: < 1 } => $"[{distance.Seconds}s ago]",
        { TotalHours: < 1 } => $"[{distance.Minutes}m ago]",
        { TotalDays: < 1 } => $"[{distance.TotalHours:F1}h ago]",
        _ => null
    };

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