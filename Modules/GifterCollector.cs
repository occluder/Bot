using Bot.Models;
using MiniTwitch.Irc.Enums;
using MiniTwitch.Irc.Interfaces;

namespace Bot.Modules;

internal class GifterCollector: BotModule
{
    private static readonly ILogger _logger = ForContext<GifterCollector>();

    private async ValueTask OnGiftedSubNoticeIntro(IGiftSubNoticeIntro notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
            return;

        _logger.Debug("@{User} gifted {Amount} {Tier} subs to #{Channel}!",
            notice.Author.Name, notice.GiftCount, notice.SubPlan, notice.Channel.Name);

        await PostgresQueryLock.WaitAsync();
        try
        {
            await Postgres.ExecuteAsync(
                """
                insert into 
                    sub_gifter 
                values (
                    @GiftId,
                    @Username, 
                    @UserId, 
                    @Channel, 
                    @ChannelId, 
                    @GiftAmount, 
                    @Tier, 
                    @TimeSent
                )
                """,
                new
                {
                    GiftId = (double)notice.CommunityGiftId,
                    Username = notice.Author.Name,
                    UserId = notice.Author.Id,
                    Channel = notice.Channel.Name,
                    ChannelId = notice.Channel.Id,
                    GiftAmount = notice.GiftCount,
                    Tier = notice.SubPlan switch
                    {
                        SubPlan.Tier1 => 1,
                        SubPlan.Tier2 => 2,
                        SubPlan.Tier3 => 3,
                        _ => 0
                    },
                    TimeSent = notice.SentTimestamp.ToUnixTimeSeconds()
                }, commandTimeout: 10
            );

            if (_gifts.Count < 1)
            {
                return;
            }

            await Postgres.ExecuteAsync(
                """
                insert into
                    sub_recipient
                values (
                    @GiftId,
                    @RecipientName,
                    @RecipientId
                )
                """,
                _gifts.Where(x => x.TmiSentTs < UnixMs() - 60000).Select(x => new
                {
                    GiftId = (double)x.CommunityGiftId,
                    RecipientName = x.Recipient.Name,
                    RecipientId = x.Recipient.Id
                }), commandTimeout: 10
            );

            _gifts.Clear();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error inserting gifter");
        }
        finally
        {
            _ = PostgresQueryLock.Release();
        }
    }

    private readonly List<IGiftSubNotice> _gifts = new(250);

    private ValueTask OnGiftedSubNotice(IGiftSubNotice notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
        {
            return default;
        }

        _logger.Verbose(
            "@{User} received a {Tier} sub to #{Channel} from @{Gifter}!",
            notice.Recipient.Name,
            notice.SubPlan,
            notice.Channel.Name,
            notice.Author.Name
        );

        _gifts.Add(notice);
        return default;
    }

    protected override ValueTask OnModuleEnabled()
    {
        MainClient.OnGiftedSubNoticeIntro += OnGiftedSubNoticeIntro;
        AnonClient.OnGiftedSubNoticeIntro += OnGiftedSubNoticeIntro;
        MainClient.OnGiftedSubNotice += OnGiftedSubNotice;
        AnonClient.OnGiftedSubNotice += OnGiftedSubNotice;
        return default;
    }

    protected override ValueTask OnModuleDisabled()
    {
        MainClient.OnGiftedSubNoticeIntro -= OnGiftedSubNoticeIntro;
        AnonClient.OnGiftedSubNoticeIntro -= OnGiftedSubNoticeIntro;
        MainClient.OnGiftedSubNotice -= OnGiftedSubNotice;
        AnonClient.OnGiftedSubNotice -= OnGiftedSubNotice;
        return default;
    }
}
