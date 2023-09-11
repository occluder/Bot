using System.Text.Json;
using Bot.Models;
using Bot.Utils;
using CachingFramework.Redis.Contracts.RedisObjects;
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
        _timer = new(TimeSpan.FromMinutes(30), Commit, PostgresQueryLock);
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
            notice.SubPlan.ToString(),
            notice.SentTimestamp.DateTime
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

        List<Sub> subs = new((int)length);
        subs.AddRange(from value in await RedisDatabaseAsync.ListRangeAsync(KEY)
            select JsonSerializer.Deserialize<Sub>(value.ToString()));

        _logger.Debug("Attempting to insert {SubCount} subs", subs.Count);
        try
        {
            int inserted = await Postgres.ExecuteAsync(
                "insert into collected_subs values (@FromUser, @FromUserId, @ToChannel, @ToChannelId, @CumulativeMonths, @Tier, @TimeSent)",
                subs);

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