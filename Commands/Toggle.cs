using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class Toggle : ChatCommand
{
    public override CommandInfo Info => new(
        "toggle",
        "Toggles modules",
        TimeSpan.Zero,
        CommandPermission.Whitelisted);

    public Toggle()
    {
        AddArgument(new("Module", 1, typeof(string)));
        AddArgument(new("EnabledOrDisable", 2, typeof(string)));
    }

    public override async ValueTask Run(Privmsg message)
    {
        ValueTask check = CheckArguments(message);
        if (!check.IsCompleted)
        {
            await check;
            return;
        }

        var module = GetArgument<string>("Module");
        var toggle = GetArgument<string>("EnabledOrDisable");
        if (toggle is not "enable" and not "disable")
        {
            await message.ReplyWith("Argument 2 must be either \"enable\" or \"disable\"");
        }

        bool enable = "toggle" == "enable";
        bool success;
        if (enable)
            success = await Module.EnableModule(module);
        else
            success = await Module.DisableModule(module);

        if (success)
            await message.ReplyWith("👍");
        else
            await message.ReplyWith("❌");
    }
}
