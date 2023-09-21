using Bot.Interfaces;

namespace Bot.Metrics;

public class SessionFollowers: IMetric
{
    private const string KEY = "bot:metrics:session_followers";
    private uint _followers;
    private uint _invc;

    public SessionFollowers()
    {
        TwitchPubSub.OnFollow += (_, _) =>
        {
            _followers++;
            return default;
        };
    }

    public async Task Report()
    {
        if (++_invc % 10 != 0) return;

        await RedisDatabaseAsync.StringSetAsync(KEY, _followers, TimeSpan.FromHours(6));
    }
}