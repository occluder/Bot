using Bot.Interfaces;
using MiniTwitch.Irc.Models;

namespace Bot.Metrics;

public class ChannelMessages: IMetric
{
    private readonly Dictionary<long, int> _messageCount = new();
    private uint _invc;

    public ChannelMessages()
    {
        AnonClient.OnMessage += OnMessage;
        MainClient.OnMessage += OnMessage;
    }

    private ValueTask OnMessage(Privmsg arg)
    {
        _messageCount.TryAdd(arg.Channel.Id, 0);
        _messageCount[arg.Channel.Id]++;
        return default;
    }

    public async Task Report()
    {
        if (++_invc % 20 != 0)
            return;

        await PostgresQueryLock.WaitAsync();
        try
        {
            (string Channel, int MessageCount)[] values =
                _messageCount.Select(kvp => (ChannelsById[kvp.Key].Username, kvp.Value)).ToArray();

            await Postgres.ExecuteAsync("insert into metrics_channel_messages values (@Channel, @MessageCount)",
                values);
        }
        catch (Exception ex)
        {
            ForContext<ChannelMessages>().Error(ex, "Something went wrong {InvocationCount}", _invc);
        }
        finally
        {
            PostgresQueryLock.Release();
        }

        _messageCount.Clear();
    }
}