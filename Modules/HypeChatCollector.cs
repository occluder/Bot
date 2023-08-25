using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class HypeChatCollector: BotModule
{
    private async ValueTask OnMessage(Privmsg message)
    {
        if (!message.HypeChat.HasContent || !ChannelsById[message.Channel.Id].IsLogged)
            return;

        ForContext<HypeChatCollector>().Verbose("@{User} sent {Amount} {Currency} through hype chat in #{Channel}!",
            message.Author.Name, GetActualAmount(message.HypeChat), message.HypeChat.PaymentCurrency, message.Channel.Name);

        await PostgresQueryLock.WaitAsync();
        try
        {
            _ = await Postgres.ExecuteAsync("insert into collected_hype_chat values (@SentBy, @SentById, @SentTo, @SentToId, @Amount, @Currency)", new
            {
                SentBy = message.Author.Name,
                SentById = message.Author.Id,
                SentTo = message.Channel.Name,
                SentToId = message.Channel.Id,
                Amount = GetActualAmount(message.HypeChat),
                Currency = message.HypeChat.PaymentCurrency
            }, commandTimeout: 10);
        }
        finally
        {
            _ = PostgresQueryLock.Release();
        }
    }

    private static double GetActualAmount(HypeChat hc) => hc.PaidAmount * Math.Pow(10, -hc.Exponent);

    protected override ValueTask OnModuleEnabled()
    {
        MainClient.OnMessage += OnMessage;
        AnonClient.OnMessage += OnMessage;
        return default;
    }
    protected override ValueTask OnModuleDisabled()
    {
        MainClient.OnMessage -= OnMessage;
        AnonClient.OnMessage -= OnMessage;
        return default;
    }
}
