using Bot.Interfaces;

namespace Bot.Metrics;

public class MemoryUsage: IMetric
{
    private const string KEY = "bot:metrics:memory_usage";

    public Task Report() => RedisDatabaseAsync.StringSetAsync(KEY, GC.GetTotalMemory(false), TimeSpan.FromMinutes(5));
}