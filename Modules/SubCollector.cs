using Bot.Models;
using Bot.Utils;
using CachingFramework.Redis.Contracts.RedisObjects;
using MiniTwitch.Irc.Enums;
using MiniTwitch.Irc.Interfaces;

namespace Bot.Modules;

public class SubCollector: BotModule
{
    private const string KEY = "bot:chat:sub_list";
    private static IRedisList<Sub> Subs => Collections.GetRedisList<Sub>(KEY);
    private static readonly ILogger _logger = ForContext<SubCollector>();
    private readonly BackgroundTimer _timer;

    public SubCollector()
    {
        _timer = new(TimeSpan.FromMinutes(60), Commit, PostgresQueryLock);
    }

    private static async ValueTask OnSub(ISubNotice notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
            return;

        Sub sub = new(
            notice.Author.Name,
            notice.Author.Id,
            notice.Channel.Name,
            notice.Channel.Id,
            notice.CumulativeMonths,
            notice.SubPlan switch
            {
                SubPlan.Tier1 => 1,
                SubPlan.Tier2 => 2,
                SubPlan.Tier3 => 3,
                SubPlan.Prime => 4,
                _ => 0
            },
            notice.SentTimestamp.ToUnixTimeSeconds()
        );

        await Subs.AddAsync(sub);
        _logger.Verbose("New sub: {@SubData}", sub);
    }

    private async Task Commit()
    {
        _logger.Verbose("Committing collected subs...");
        long length = await RedisDatabaseAsync.ListLengthAsync(KEY);
        if (!this.Enabled || length == 0)
            return;

        Sub[] subs = (await Collections.GetRedisList<Sub>(KEY).GetRangeAsync()).ToArray();
        _logger.Debug("Attempting to insert {SubCount} subs", subs.Length);
        try
        {
            int inserted = await Postgres.ExecuteAsync(
                "insert into subscriptions values " +
                "(@Username, @UserId, @Channel, @ChannelId, @CumulativeMonths, @Tier, @TimeSent)",
                subs
            );

            _logger.Debug("{InsertedCount} subs inserted", inserted);
            await Subs.ClearAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to insert sub logs");
        }
    }

    protected override ValueTask OnModuleEnabled()
    {
        MainClient.OnSubscriptionNotice += OnSub;
        AnonClient.OnSubscriptionNotice += OnSub;
        _timer.Start();
        return default;
    }

    protected override async ValueTask OnModuleDisabled()
    {
        MainClient.OnSubscriptionNotice -= OnSub;
        AnonClient.OnSubscriptionNotice -= OnSub;
        await _timer.StopAsync();
    }

    private record Sub(
        string Username,
        long UserId,
        string Channel,
        long ChannelId,
        int CumulativeMonths,
        int Tier,
        long TimeSent
    );
}