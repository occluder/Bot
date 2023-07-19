using Bot.Interfaces;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class HypeChatCollector : IModule
{
    public bool Enabled { get; private set; }

    private async ValueTask OnMessage(Privmsg message)
    {
        if (!message.HypeChat.HasContent || !ChannelsById[message.Channel.Id].IsLogged)
            return;

        ForContext<HypeChatCollector>().Verbose("@{User} sent {Amount} {Currency} through hype chat in #{Channel}!",
            message.Author.Name, GetActualAmount(message.HypeChat), message.HypeChat.PaymentCurrency, message.Channel.Name);

        await PostgresTimerSemaphore.WaitAsync();
        try
        {
            await Postgres.ExecuteAsync("insert into collected_hype_chat values (@sent_by, @sent_by_id, @sent_to, @sent_to_id, @amount, @currency)", new
            {
                sent_by = message.Author.Name,
                sent_by_id = message.Author.Id,
                sent_to = message.Channel.Name,
                sent_to_id = message.Channel.Id,
                amount = GetActualAmount(message.HypeChat),
                currency = message.HypeChat.PaymentCurrency
            }, commandTimeout: 10);
        }
        finally
        {
            _ = PostgresTimerSemaphore.Release();
        }
    }

    private double GetActualAmount(HypeChat hc) => hc.PaidAmount * Math.Pow(10, -hc.Exponent);

    public async ValueTask Enable()
    {
        if (this.Enabled)
            return;

        MainClient.OnMessage += OnMessage;
        AnonClient.OnMessage += OnMessage;
        this.Enabled = true;
        await Settings.EnableModule(nameof(HypeChatCollector));
    }

    public async ValueTask Disable()
    {
        if (!this.Enabled)
            return;

        MainClient.OnMessage -= OnMessage;
        AnonClient.OnMessage -= OnMessage;
        this.Enabled = false;
        await Settings.DisableModule(nameof(HypeChatCollector));
    }
}
