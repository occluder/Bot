using Bot.Models;
using MiniTwitch.Irc.Enums;
using MiniTwitch.Irc.Interfaces;

namespace Bot.Modules;

internal class GifterCollector : BotModule
{
    private async ValueTask OnGiftedSubNoticeIntro(IGiftSubNoticeIntro notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
            return;

        ForContext<HypeChatCollector>().Verbose("@{User} gifted {Amount} {Tier} subs to #{Channel}!",
            notice.Author.Name, notice.GiftCount, notice.SubPlan, notice.Channel.Name);

        await PostgresQueryLock.WaitAsync();
        try
        {
            await Postgres.ExecuteAsync("insert into collected_gifts values (@SentBy, @SentById, @SentTo, @SentToId, @GiftAmount, @Tier, @TimeSent)", new
            {
                SentBy = notice.Author.Name,
                SentById = notice.Author.Id,
                SentTo = notice.Channel.Name,
                SentToId = notice.Channel.Id,
                GiftAmount = notice.GiftCount,
                Tier = notice.SubPlan switch
                {
                    SubPlan.Tier1 => 1,
                    SubPlan.Tier2 => 2,
                    SubPlan.Tier3 => 3,
                    _ => 0
                },
                TimeSent = notice.SentTimestamp
            }, commandTimeout: 10);
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
        return default;
    }
    protected override ValueTask OnModuleDisabled()
    {
        MainClient.OnGiftedSubNoticeIntro -= OnGiftedSubNoticeIntro;
        AnonClient.OnGiftedSubNoticeIntro -= OnGiftedSubNoticeIntro;
        return default;
    }
}
