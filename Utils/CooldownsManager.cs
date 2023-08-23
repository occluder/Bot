using Bot.Interfaces;
using MiniTwitch.Irc.Models;

internal static class CooldownsManager
{
    private static readonly Dictionary<string, Dictionary<long, long>> _cooldowns = new();
    private static readonly Dictionary<long, long> _channelCooldowns = new();

    public static bool IsOnCooldown(this Privmsg privmsg, IChatCommand command)
    {
        long timeNow = DateTimeOffset.Now.ToUnixTimeSeconds();
        // No one used the command before, so there is no cooldown
        if (!_cooldowns.ContainsKey(command.Info.Name))
            _cooldowns[command.Info.Name] = new();

        if (!_cooldowns[command.Info.Name].ContainsKey(privmsg.Author.Id)) // User didn't use the command before, so there is no cooldown
            _cooldowns[command.Info.Name][privmsg.Author.Id] = timeNow;
        else if (command.Info.Cooldown.TotalSeconds > timeNow - _cooldowns[command.Info.Name][privmsg.Author.Id]) // On cooldown
            return true;

        if (!_channelCooldowns.ContainsKey(privmsg.Channel.Id))
            _channelCooldowns[privmsg.Channel.Id] = timeNow;
        else if (3 > timeNow - _channelCooldowns[privmsg.Channel.Id])
            return true;

        ForContext("NewTimeStamp", timeNow).ForContext("OldTimeStamp", _cooldowns[command.Info.Name][privmsg.Author.Id])
            .Verbose("Cooldown updated for user id {UserId}", privmsg.Author.Id);

        _cooldowns[command.Info.Name][privmsg.Author.Id] = timeNow;
        _channelCooldowns[privmsg.Channel.Id] = timeNow;
        return false;
    }
}