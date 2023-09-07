using System.Diagnostics;
using Bot.Interfaces;

namespace Bot.Metrics;

public class MemoryUsage: IMetric
{
    private const string KEY = "bot:metrics:memory_usage";

    public Task Report() => Cache.SetObjectAsync(KEY,
        new { GCHeapSize = GC.GetTotalMemory(false), ProcessSize = Process.GetCurrentProcess().PrivateMemorySize64 },
        TimeSpan.FromMinutes(5));
}