using Bot.Enums;
using Bot.Interfaces;
using MiniTwitch.Irc.Models;

namespace Bot.Utils;

internal static class PermissionChecker
{
    public static bool Permits(this Privmsg message, IChatCommand command)
    {
        CommandPermission level;
        if (WhiteListedUserIds.Contains(message.Author.Id))
            level = CommandPermission.Whitelisted;
        else if (message.Author.IsMod)
            level = CommandPermission.Moderators;
        else if (message.Author.IsVip)
            level = CommandPermission.VIPs;
        else
        {
            level = message.Author.IsSubscriber
            ? CommandPermission.Subscribers
            : BlackListedUserIds.Contains(message.Author.Id) ? CommandPermission.None : CommandPermission.Everyone;
        }

        bool hasPerms = level >= command.Info.Permission;
        ForContext("Permission", level).Verbose("{User} {HasPerms} run command: {Command}", message.Author.Name, hasPerms ? "can" : "can't",
            command.Info.Name);

        return hasPerms;
    }
}