using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

internal class Ping: IChatCommand
{
    public CommandInfo Info => new("ping", "Ping", TimeSpan.FromSeconds(5), CommandPermission.Everyone);

    public ValueTask Run(Privmsg message)
    {
        TimeSpan latency = DateTimeOffset.Now - message.SentTimestamp;
        return MainClient.SendMessage(message.Channel.Name, $"FeelsDankMan Pong {latency.TotalMilliseconds}ms");
    }
}