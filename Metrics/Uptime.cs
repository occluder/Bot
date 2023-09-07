using Bot.Interfaces;

namespace Bot.Metrics;

public class Uptime: IMetric
{
    private const string KEY = "bot:metrics:uptime";
    private readonly DateTime _start = DateTime.Now;

    public Task Report() =>
        RedisDatabaseAsync.StringSetAsync(KEY, (int)GetUptime().TotalHours, TimeSpan.FromMinutes(5));

    private TimeSpan GetUptime() => DateTime.Now - _start;
}