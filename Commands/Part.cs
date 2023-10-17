using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;
internal class Part: ChatCommand
{
    public override CommandInfo Info { get; } = new(
        "part",
        "Leave a channel",
        TimeSpan.Zero,
        CommandPermission.Whitelisted
    );

    public Part()
    {
        AddArgument(new("ChannelId", 1, typeof(long)));
    }

    public override async ValueTask Run(Privmsg message)
    {
        long id = GetArgument<long>("ChannelId");
        if (ChannelsById.ContainsKey(id))
        {
            await PartChannel(id);
            await message.ReplyWith("👍");
            return;
        }

        await message.ReplyWith("Failed");
    }
}
