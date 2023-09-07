using Bot.Interfaces;

namespace Bot.Metrics;

public class ActiveTopics: IMetric
{
    private const string KEY = "bot:metrics:pubsub_topics";

    public Task Report() =>
        RedisDatabaseAsync.StringSetAsync(KEY, TwitchPubSub.ActiveTopics.Count, TimeSpan.FromMinutes(5));
}