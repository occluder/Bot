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
        if (++_invc % 4 != 0)
            return;

        using var conn = await NewDbConnection();
        try
        {
            Point[] values = [..
                _messageCount
                .Where(x => ChannelsById.ContainsKey(x.Key))
                .Select(kvp => new Point(ChannelsById[kvp.Key].ChannelName, kvp.Value))
            ];

            await conn.ExecuteAsync(
                "insert into metrics_channel_messages values (@Channel, @MessageCount)",
                values
            );
        }
        catch (Exception ex)
        {
            ForContext<ChannelMessages>().Error(ex, "Something went wrong {InvocationCount}", _invc);
        }

        _messageCount.Clear();
    }
}

file readonly record struct Point(string Channel, int MessageCount);