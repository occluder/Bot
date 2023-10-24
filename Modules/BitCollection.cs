using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class BitCollection: BotModule
{
    private async ValueTask OnMessage(Privmsg message)
    {
        if (message.Bits == 0 || !ChannelsById[message.Channel.Id].IsLogged)
            return;

        ForContext<BitCollection>().Verbose("@{User} sent {Amount} bits to #{Channel}!", message.Author.Name,
            message.Bits, message.Channel.Name);
        await PostgresQueryLock.WaitAsync();
        try
        {
            _ = await Postgres.ExecuteAsync(
                "insert into bits_users values (@Username, @UserId, @Channel, @ChannelId, @BitAmount, @TimeSent)",
                new
                {
                    Username = message.Author.Name,
                    UserId = message.Author.Id,
                    Channel = message.Channel.Name,
                    ChannelId = message.Channel.Id,
                    BitAmount = message.Bits,
                    TimeSent = message.SentTimestamp
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
