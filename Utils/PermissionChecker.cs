using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Utils;

internal static class PermissionChecker
{
    public static bool Permits(this Privmsg message, IChatCommand command)
    {
        var level = CommandPermission.Everyone;
        if (UserPermissions.TryGetValue(message.Author.Id, out UserPermissionDto? perms))
        {
            if (perms.IsWhitelisted)
                level = CommandPermission.Whitelisted;
            else if (perms.IsBlacklisted)
                level = CommandPermission.None;
        }
        else
        {
            if (message.Author.IsMod)
                level = CommandPermission.Moderators;
            else if (message.Author.IsVip)
                level = CommandPermission.VIPs;
            else if (message.Author.IsSubscriber)
                level = CommandPermission.Subscribers;
        }

        return level >= command.Info.Permission;
    }
}