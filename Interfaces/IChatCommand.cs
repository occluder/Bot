using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Interfaces;

public interface IChatCommand
{
    public CommandInfo Info { get; }
    public ValueTask Run(Privmsg message);
}
