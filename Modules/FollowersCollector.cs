using Bot.Models;
using CachingFramework.Redis.Contracts.RedisObjects;
using MiniTwitch.PubSub.Models;
using MiniTwitch.PubSub.Payloads;

namespace Bot.Modules;

public class FollowersCollector: BotModule
{
    private const int MAX_BATCH_SIZE = 25;
    private const int MAX_REDIS_LIST_SIZE = MAX_BATCH_SIZE * 100;

    private static IRedisList<FollowData> Followers => Collections.GetRedisList<FollowData>("bot:chat:follow_list");
    private static readonly ILogger _logger = ForContext<FollowersCollector>();
    private readonly List<FollowData> _batch = new(MAX_BATCH_SIZE);

    private async ValueTask OnFollow(ChannelId channelId, Follower follower)
    {
        _batch.Add((channelId, follower));
        _logger.Verbose("{@Data}", follower);

        if (_batch.Count < MAX_BATCH_SIZE) return;
        await Followers.AddRangeAsync(_batch);
        _logger.Debug("{FollowerCount} followers added to Redis", _batch.Count);
        _batch.Clear();

        if (Followers.Count < MAX_REDIS_LIST_SIZE) return;
        FollowData[] redisFollowers = (await Followers.GetRangeAsync()).ToArray();
        await PostgresQueryLock.WaitAsync();
        try
        {
            int inserted = await Postgres.ExecuteAsync(
                "insert into collected_users" +
                " values (@Username, @UserId, @ChannelName, @TimeFollowed)", redisFollowers);

            _logger.Debug("Inserted {UserCount} followers into {TableName}", inserted, "collected_users");
            await Followers.ClearAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to collect followers");
        }
        finally
        {
            PostgresQueryLock.Release();
        }
    }

    protected override async ValueTask OnModuleEnabled()
    {
        TwitchPubSub.OnFollow += OnFollow;
        foreach (TwitchChannelDto channel in Channels.Values.Where(c => c.WatchFollows))
            await TwitchPubSub.ListenTo(Topics.Following(channel.Id));
    }

    protected override async ValueTask OnModuleDisabled()
    {
        TwitchPubSub.OnFollow -= OnFollow;
        foreach (TwitchChannelDto channel in Channels.Values.Where(c => c.WatchFollows))
            await TwitchPubSub.UnlistenTo(Topics.Following(channel.Id));
    }

    private readonly record struct FollowData(string Username, long UserId, string ChannelName, DateTime TimeFollowed)
    {
        public static implicit operator FollowData((ChannelId c, Follower f) t) =>
            new(t.f.Name, t.f.Id, ChannelsById[t.c].Username, DateTime.Now);
    }
}