using Bot.Interfaces;

namespace Bot.Metrics;

public class Queries: IMetric
{
    private const string KEY = "bot:metrics:queries";
    public static uint Count;
    public Task Report() => RedisDatabaseAsync.StringSetAsync(KEY, Count / 2, TimeSpan.FromMinutes(5));
}