using Bot.Enums;
using Bot.Handlers;
using Bot.Interfaces;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class Help: ChatCommand
{
    public override CommandInfo Info { get; } = new(
        "help",
        "Displays information about commands",
        TimeSpan.FromSeconds(5),
        CommandPermission.Everyone
    );

    public Help()
    {
        AddArgument(new("CommandName", 1, typeof(string), true));
    }

    public override ValueTask Run(Privmsg message)
    {
        IChatCommand[] commands = ChatHandler.GetCommands().ToArray();
        if (!TryGetArgument("CommandName", out string commandName))
            return message.ReplyWith($"All commands: {string.Join(", ", commands.Select(x => x.Info.Name))}");

        if (commands.FirstOrDefault(x => x.Info.Name == commandName) is { } cmd)
            return message.ReplyWith(
                $"{cmd.Info.Name}: {cmd.Info.Description}, cooldown: {cmd.Info.Cooldown}, permission: {cmd.Info.Permission}");

        return message.ReplyWith("Command not found");
    }
}