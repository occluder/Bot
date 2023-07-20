using Bot.Interfaces;
using MiniTwitch.Irc.Interfaces;

namespace Bot.Modules;

internal class GifterCollector : IModule
{
    public bool Enabled { get; private set; }

    private async ValueTask OnGiftedSubNoticeIntro(IGiftSubNoticeIntro notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
            return;

        ForContext<HypeChatCollector>().Verbose("@{User} gifted {Amount} {Tier} subs to #{Channel}!",
            notice.Author.Name, notice.GiftCount, notice.SubPlan, notice.Channel.Name);

        await PostgresTimerSemaphore.WaitAsync();
        try
        {
            await Postgres.ExecuteAsync("insert into collected_gifts values (@SentBy, @SentById, @SentTo, @SentToId, @GiftAmount, @Tier, @TimeSent)", new
            {
                SentBy = notice.Author.Name,
                SentById = notice.Author.Id,
                SentTo = notice.Channel.Name,
                SentToId = notice.Channel.Id,
                GiftAmount = notice.GiftCount,
                Tier = notice.SubPlan.ToString(),
                TimeSent = notice.SentTimestamp
            }, commandTimeout: 10);
        }
        finally
        {
            _ = PostgresTimerSemaphore.Release();
        }
    }

    public async ValueTask Enable()
    {
        if (this.Enabled)
            return;

        MainClient.OnGiftedSubNoticeIntro += OnGiftedSubNoticeIntro;
        AnonClient.OnGiftedSubNoticeIntro += OnGiftedSubNoticeIntro;
        this.Enabled = true;
        await Settings.EnableModule(nameof(GifterCollector));
    }

    public async ValueTask Disable()
    {
        if (!this.Enabled)
            return;

        MainClient.OnGiftedSubNoticeIntro -= OnGiftedSubNoticeIntro;
        AnonClient.OnGiftedSubNoticeIntro -= OnGiftedSubNoticeIntro;
        this.Enabled = false;
        await Settings.DisableModule(nameof(GifterCollector));
    }
}
