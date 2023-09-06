using Bot.Models;
using Bot.Utils;
using CachingFramework.Redis.Contracts.RedisObjects;
using MiniTwitch.Irc.Interfaces;

namespace Bot.Modules;

public class SubCollector: BotModule
{
    private static IRedisSet<Sub> Subs => Collections.GetRedisSet<Sub>("bot:chat:sub_notices", 100);
    private readonly BackgroundTimer _timer;

    public SubCollector()
    {
        _timer = new(TimeSpan.FromHours(1), Commit, PostgresQueryLock);
    }

    private async ValueTask OnSub(ISubNotice notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
            return;

        Sub sub = new(
            notice.Author.Name,
            notice.Author.Id,
            notice.Channel.Name,
            notice.Channel.Id,
            notice.CumulativeMonths,
            notice.SubPlan.ToString(),
            notice.SentTimestamp.DateTime
        );

        _ = await Subs.AddAsync(sub);
        ForContext<SubCollector>().Verbose("New sub: {@SubData}", sub);
    }

    private async Task Commit()
    {
        if (!base.Enabled || await Subs.GetCountAsync() == 0)
            return;

        Sub[] subs = Subs.ToArray();
        try
        {
            int inserted = await Postgres.ExecuteAsync("insert into collected_subs values (@FromUser, @FromUserId, @ToChannel, @ToChannelId, @CumulativeMonths, @Tier, @TimeSent)", subs);
            ForContext<SubCollector>().Debug("{InsertedCount} subs inserted", inserted);
            await Subs.ClearAsync();
        }
        catch (Exception ex)
        {
            ForContext<SubCollector>().Error(ex, "Failed to insert sub logs");
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

    private readonly record struct Sub(
        string FromUser,
        long FromUserId,
        string ToChannel,
        long ToChannelId,
        int CumulativeMonths,
        string Tier,
        DateTime TimeSent
    );
}
