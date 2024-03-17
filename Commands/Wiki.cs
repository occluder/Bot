using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class Wiki: ChatCommand
{
    public override CommandInfo Info { get; } = new(
        "wiki",
        "Search the Warframe wiki",
        TimeSpan.FromSeconds(1),
        CommandPermission.Everyone
    );

    public Wiki()
    {
        AddArgument(new("SearchTerm", 1, typeof(string)));
    }

    public override ValueTask Run(Privmsg message)
    {
        string[] s = message.Content.Split(' ');
        return message.ReplyWith($"https://antifandom.com/warframe/search?q={string.Join('+', s[1..])}");
    }
}
