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
        AddArgument(new("Search Term", typeof(string), TakeRemaining: true));
    }

    private const string WIKI_URL = "https://wiki.warframe.com/w/Special:Search?search=";

    public override async ValueTask Run(Privmsg message)
    {
        await message.ReplyWith($"{WIKI_URL}{GetArgument("Search Term").AssumedString}");
    }
}
