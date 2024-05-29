using Bot.Models;
using MiniTwitch.Irc.Enums;
using MiniTwitch.Irc.Interfaces;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class GifterCollector: BotModule
{
    private async ValueTask OnGiftedSubNoticeIntro(IGiftSubNoticeIntro notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
            return;

        ForContext<GifterCollector>().Debug("@{User} gifted {Amount} {Tier} subs to #{Channel}!",
            notice.Author.Name, notice.GiftCount, notice.SubPlan, notice.Channel.Name);

        await PostgresQueryLock.WaitAsync();
        try
        {
            await Postgres.ExecuteAsync(
                """
                insert into 
                    sub_gifters 
                values (
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
        }
        finally
        {
            _ = PostgresQueryLock.Release();
        }
    }

    private async ValueTask OnGiftedSubNotice(IGiftSubNotice notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
        {
            return;
        }

        ForContext<GifterCollector>().Verbose(
            "@{User} received a {Tier} sub to #{Channel} from @{Gifter}!",
            notice.Recipient.Name,
            notice.SubPlan,
            notice.Channel.Name,
            notice.Author.Name
        );

        await PostgresQueryLock.WaitAsync();
        try
        {
            await Postgres.ExecuteAsync(
                """
                insert into
                    sub_recipient
                values (
                    @ChannelName,
                    @ChannelId,
                    @GifterName,
                    @GifterId,
                    @RecipientName,
                    @RecipientId,
                    @Tier,
                    @TimeSent
                )
                """,
                new
                {
                    ChannelName = notice.Channel.Name,
                    ChannelId = notice.Channel.Id,
                    GifterName = notice.Author.Name,
                    GifterId = notice.Author.Id,
                    RecipientName = notice.Recipient.Name,
                    RecipientId = notice.Recipient.Id,
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
        }
        finally
        {
            _ = PostgresQueryLock.Release();
        }
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
