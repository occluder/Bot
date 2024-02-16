using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Utils;

internal static class PermissionChecker
{
    public static bool Permits(this Privmsg message, IChatCommand command)
    {
        CommandPermission level = CommandPermission.Everyone;
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

        Verbose("{User} ({Permission}) trying to use command: {Command} ({Required}) = {Result}",
            message.Author.Name, level, command.Info.Name, command.Info.Permission, level >= command.Info.Permission);

        return level >= command.Info.Permission;
    }
}