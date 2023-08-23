using Bot.Enums;
using Bot.Interfaces;
using MiniTwitch.Irc.Models;

namespace Bot.Utils;

internal static class PermissionChecker
{
    public static bool Permits(this Privmsg message, IChatCommand command)
    {
        CommandPermission level = WhiteListedUserIds.Contains(message.Author.Id)
            ? CommandPermission.Whitelisted
            : message.Author.IsMod
            ? CommandPermission.Moderators
            : message.Author.IsVip
            ? CommandPermission.VIPs
            : message.Author.IsSubscriber
            ? CommandPermission.Subscribers
            : BlackListedUserIds.Contains(message.Author.Id) ? CommandPermission.None : CommandPermission.Everyone;

        bool hasPerms = level >= command.Info.Permission;
        ForContext("Permission", level).Verbose("{User} {HasPerms} run command: {Command}", message.Author.Name, hasPerms ? "can" : "can't",
            command.Info.Name);

        return hasPerms;
    }
}