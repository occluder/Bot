using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class Toggle: ChatCommand
{
    public override CommandInfo Info => new("toggle", "Toggles modules", TimeSpan.Zero, CommandPermission.Whitelisted);

    public Toggle()
    {
        AddArgument(new("Module", 1, typeof(string)));
    }

    public override async ValueTask Run(Privmsg message)
    {
        string module = GetArgument<string>("Module");
        bool success = Module.Exists(module) && Module.IsEnabled(module)
            ? await Module.DisableModule(module)
            : await Module.EnableModule(module);

        if (success)
            await message.ReplyWith("👍");
        else
            await message.ReplyWith("❌");
    }
}