using Bot.Models;
using CachingFramework.Redis.Contracts.RedisObjects;
using MiniTwitch.PubSub.Models;
using MiniTwitch.PubSub.Payloads;

namespace Bot.Modules;

public class FollowersCollector: BotModule
{
    private const int MAX_REDIS_LIST_SIZE = 25;

    private static IRedisList<FollowData> Followers => Collections.GetRedisList<FollowData>("bot:chat:follow_list");
    private static readonly ILogger _logger = ForContext<FollowersCollector>();
    private int _inserted;

    private async ValueTask OnFollow(ChannelId channelId, Follower follower)
    {
        _logger.Verbose("New follower: {@Data}", follower);
        await Followers.AddAsync((channelId, follower));
        _inserted++;

        if (_inserted % 50 == 0 && Followers.Count < MAX_REDIS_LIST_SIZE) return;
        FollowData[] redisFollowers = (await Followers.GetRangeAsync()).ToArray();
        _logger.Verbose("Attempting to insert {FollowerCount} followers", redisFollowers.Length);
        await PostgresQueryLock.WaitAsync();
        try
        {
            int inserted = await Postgres.ExecuteAsync(
                "insert into collected_users values (@Username, @UserId, @ChannelName, @TimeFollowed) " +
                "on conflict on constraint pk_users do nothing", redisFollowers);

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