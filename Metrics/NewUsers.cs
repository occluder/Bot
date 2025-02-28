using Bot.Interfaces;

namespace Bot.Metrics;

public class NewUsers: IMetric
{
    private const string KEY = "bot:metrics:startup";
    private uint _invc;
    private readonly long _start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public async Task Report()
    {
        if (++_invc % 20 != 0)
            return;

        await RedisDatabaseAsync.StringSetAsync(KEY, _start, TimeSpan.FromHours(6));
    }
}