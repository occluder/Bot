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

    private const string WIKI_URL = "https://wiki.warframe.com/w/Special:Search?search=";

    public override async ValueTask Run(Privmsg message)
    {
        string[] split = message.Content.Split(' ');
        for (int i = 1; i < split.Length; i++)
        {
            if (split[i].Length < 2)
            {
                continue;
            }

            split[i] = char.ToUpper(split[i][0]) + split[i][1..];
        }

        await message.ReplyWith($"{WIKI_URL}{string.Join('+', split[1..])}");
    }
}
