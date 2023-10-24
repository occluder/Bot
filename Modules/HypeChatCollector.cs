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
            _ = await Postgres.ExecuteAsync(
                "insert into hype_chat values (@Username, @UserId, @Channel, @ChannelId, @Amount, @Currency, @TimeSent)",
                new
                {
                    Username = message.Author.Name,
                    UserId = message.Author.Id,
                    Channel = message.Channel.Name,
                    ChannelId = message.Channel.Id,
                    Amount = GetActualAmount(message.HypeChat),
                    Currency = message.HypeChat.PaymentCurrency,
                    TimeSent = message.SentTimestamp.ToUnixTimeSeconds()
                }, commandTimeout: 10
            );
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
