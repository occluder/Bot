using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class Toggle : IChatCommand
{
    public CommandInfo Info => new(
        "toggle",
        "Toggles modules",
        TimeSpan.Zero,
        CommandPermission.Whitelisted);

    public async ValueTask Run(Privmsg message)
    {
        string[] args = message.Content.Split(' ');
        if (args.Length < 2)
        {
            await message.ReplyWith("Arg 2 missing: Specify a module");
            return;
        }
        else if (args.Length < 3)
        {
            await message.ReplyWith("Arg 3 missing: Specify whether to enable or disable");
            return;
        }
        else if (args[2] is not "enable" and not "disable")
        {
            await message.ReplyWith("Arg 3 error: Value must be either \"enable\" or \"disable\"");
            return;
        }

        bool enable = args[2] == "enable";
        bool success;
        if (enable)
            success = await Module.EnableModule(args[1]);
        else
            success = await Module.DisableModule(args[1]);

        if (success)
            await message.ReplyWith("👍");
        else
            await message.ReplyWith("❌");
    }
}
