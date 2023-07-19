using Bot.Interfaces;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class BitCollection : IModule
{
    public bool Enabled { get; private set; }

    private async ValueTask OnMessage(Privmsg message)
    {
        if (message.Bits == 0 || !ChannelsById[message.Channel.Id].IsLogged)
            return;

        ForContext<HypeChatCollector>().Verbose("@{User} sent {Amount} bits to #{Channel}!", message.Author.Name, message.Bits, message.Channel.Name);
        await PostgresTimerSemaphore.WaitAsync();
        try
        {
            await Postgres.ExecuteAsync("insert into collected_bits values (@SentBy, @SentById, @SentTo, @SentToId, @BitAmount, @TimeSent)", new
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
            _ = PostgresTimerSemaphore.Release();
        }
    }

    public async ValueTask Enable()
    {
        if (this.Enabled)
            return;

        MainClient.OnMessage += OnMessage;
        AnonClient.OnMessage += OnMessage;
        this.Enabled = true;
        await Settings.EnableModule(nameof(BitCollection));
    }

    public async ValueTask Disable()
    {
        if (!this.Enabled)
            return;

        MainClient.OnMessage -= OnMessage;
        AnonClient.OnMessage -= OnMessage;
        this.Enabled = false;
        await Settings.DisableModule(nameof(BitCollection));
    }
}
