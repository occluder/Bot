using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class BitCollection: BotModule
{
    private async ValueTask OnMessage(Privmsg message)
    {
        if (message.Bits == 0 || !ChannelsById[message.Channel.Id].IsLogged)
            return;

        ForContext<HypeChatCollector>().Verbose("@{User} sent {Amount} bits to #{Channel}!", message.Author.Name, message.Bits, message.Channel.Name);
        await PostgresQueryLock.WaitAsync();
        try
        {
            _ = await Postgres.ExecuteAsync("insert into collected_bits values (@SentBy, @SentById, @SentTo, @SentToId, @BitAmount, @TimeSent)", new
            {
                SentBy = message.Author.Name,
                SentById = message.Author.Id,
                SentTo = message.Channel.Name,
                SentToId = message.Channel.Id,
                BitAmount = message.Bits,
                TimeSent = message.SentTimestamp
            }, commandTimeout: 10);
        }
        finally
        {
            _ = PostgresQueryLock.Release();
        }
    }

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
