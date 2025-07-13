using System.Globalization;
using Bot.Models;
using CachingFramework.Redis.Contracts.RedisObjects;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

public class ChatUtils: BotModule
{
    private const int MAX_YEAR_OFFSET = 10;
    private static readonly Dictionary<double, TimeZoneInfo> _timeZones = new();

    private static async ValueTask OnMessage(Privmsg message)
    {
        await TimeUtils(message);
        //await NoFuckFebruary(message);
    }

    private static ValueTask TimeUtils(Privmsg message)
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
                "eest" or "ast" => message.ReplyWith(Date(180)),
                "cest" or "eet" => message.ReplyWith(Date(120)),
                "cet" => message.ReplyWith(Date(60)),
                "utc" or "gmt" => message.ReplyWith(Date()),
                "et" or "edt" => message.ReplyWith(Date(-240)),
                "pt" or "pdt" => message.ReplyWith(Date(-420)),
                "pst" => message.ReplyWith(Date(-480)),
                "unix" => message.ReplyWith(SeparatedUnixMs(UnixMs())),

                { Length: 10 } unix when long.TryParse(unix, out long time) && WithinReasonableTime(time) =>
                    message.ReplyWith(Date(unix: time)),

                { Length: 13 } unixMs when long.TryParse(unixMs, out long time) && WithinReasonableTime(time, true) =>
                    message.ReplyWith(Date(unix: time, ms: true)),

                { Length: >= 19 } date when DateTimeOffset.TryParse(date, out DateTimeOffset dateTime) =>
                    message.ReplyWith(SeparatedUnixMs(dateTime.ToUnixTimeMilliseconds())),

                { Length: 16 } and [_, _, _, _, _, _, _, _, 'T', _, _, _, _, _, _, 'Z'] ics when space == -1 =>
                    message.ReplyWith(SeparatedUnixMs(FromIcsTime(ics.ToString()).ToUnixTimeMilliseconds())),

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

            return $"{offset:yyyy-MM-dd, h:mm:ss tt, (UTCzzz)} {ShortDistance(DateTimeOffset.Now - offset)}";
        }

        var utc = DateTimeOffset.UtcNow;
        if (!_timeZones.TryGetValue(minOffset, out TimeZoneInfo? tz))
        {
            tz = TimeZoneInfo.GetSystemTimeZones().First(t => (int)t.BaseUtcOffset.TotalMinutes == (int)minOffset);
            _timeZones.Add(minOffset, tz);
        }

        var date = TimeZoneInfo.ConvertTime(utc, tz);
        return $"{date:yyyy-MM-dd, h:mm:ss tt, (UTCzzz)}";
    }

    private static bool WithinReasonableTime(long time, bool ms = false)
    {
        int year = DateTime.Now.Year;
        var date = ms ? DateTimeOffset.FromUnixTimeMilliseconds(time) : DateTimeOffset.FromUnixTimeSeconds(time);
        return date.Year <= year + MAX_YEAR_OFFSET && date.Year >= year - MAX_YEAR_OFFSET;
    }

    private static string? ShortDistance(TimeSpan distance) => distance switch
    {
        { TotalSeconds: < 0 } => null,
        { TotalMinutes: < 1 } => $"[{distance.Seconds}s ago]",
        { TotalHours: < 1 } => $"[{distance.Minutes}m ago]",
        { TotalDays: < 1 } => $"[{distance.TotalHours:F1}h ago]",
        _ => null
    };

    private static readonly IRedisList<string> _februaryList = Collections.GetRedisList<string>("bot:chat:february");

    private static async ValueTask NoFuckFebruary(Privmsg message)
    {
        if (DateTime.Now.Month != 2)
        {
            return;
        }

        if (message.Channel.Id != 11148817 || !message.Content.Contains("fuck"))
        {
            return;
        }

        for (int i = 0; i < CountFucks(message.Content); i++)
        {
            await _februaryList.AddAsync(message.Author.Name);
        }
    }

    private static int CountFucks(string message)
    {
        ReadOnlySpan<char> span = message;
        Span<Range> ranges = stackalloc Range[64];
        int splits = span.Split(ranges, ' ');
        int fucks = 0;
        for (int i = 0; i < splits; i++)
            if (span[ranges[i]].Contains("fuck", StringComparison.OrdinalIgnoreCase))
                fucks++;

        return fucks;
    }

    static DateTimeOffset FromIcsTime(string icsTime)
    {
        if (icsTime.Length < 15 || !icsTime.EndsWith("Z", StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid ICS time format.", nameof(icsTime));
        }

        if (DateTimeOffset.TryParseExact(icsTime, "yyyyMMddTHHmmssZ", null, DateTimeStyles.AssumeUniversal, out DateTimeOffset date))
        {
            return date;
        }

        throw new FormatException("Failed to parse ICS time.");
    }

    static string SeparatedUnixMs(long unixMs) => $"{unixMs / 1000.0:#.000}";

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