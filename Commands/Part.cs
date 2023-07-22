using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;
internal class Part : IChatCommand
{
    public CommandInfo Info => new("part", "Leave a channel", TimeSpan.Zero, CommandPermission.Whitelisted);

    public async ValueTask Run(Privmsg message)
    {
        string[] args = message.Content.Split(' ');
        if (args.Length < 2)
            return;

        if (!long.TryParse(args[1], out long id))
            await message.ReplyWith("First argument is user id (int64)");

        if (ChannelsById.ContainsKey(id))
        {
            await PartChannel(id);
            await message.ReplyWith("👍");
            return;
        }

        await message.ReplyWith("Failed");
    }
}
