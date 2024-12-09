using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class BitCollection: BotModule
{
    private static readonly ILogger _logger = ForContext<BitCollection>();

    private async ValueTask OnMessage(Privmsg message)
    {
        if (message.Bits == 0 || !ChannelsById[message.Channel.Id].IsLogged)
            return;

        await LiveConnectionLock.WaitAsync();
        try
        {
            _ = await LiveDbConnection.ExecuteAsync(
                "insert into bits_users values (@Username, @UserId, @Channel, @ChannelId, @BitAmount, @TimeSent)",
                new
                {
                    Username = message.Author.Name,
                    UserId = message.Author.Id,
                    Channel = message.Channel.Name,
                    ChannelId = message.Channel.Id,
                    BitAmount = message.Bits,
                    TimeSent = message.TmiSentTs / 1000
                }, commandTimeout: 10
            );

            _logger.Verbose(
                "@{User} sent {Amount} bits to #{Channel}!",
                message.Author.Name, message.Bits, message.Channel.Name
            );
        }
        finally
        {
            _ = LiveConnectionLock.Release();
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
