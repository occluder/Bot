using Bot.Interfaces;
using Bot.Utils;
using MiniTwitch.Irc.Models;

namespace Bot.Handlers;

public static class ChatHandler
{
    private static readonly Dictionary<string, IChatCommand> _commands = new();

    public static void Setup()
    {
        MainClient.OnMessage += OnMessage;
        AnonClient.OnMessage += OnMessage;

        LoadCommands();
    }

    private static void LoadCommands()
    {
        Type interfaceType = typeof(IChatCommand);
        foreach (Type type in interfaceType.Assembly.GetTypes().Where(interfaceType.IsAssignableFrom))
        {
            if (!type.IsInterface && !type.IsAbstract && Activator.CreateInstance(type) is IChatCommand command)
            {
                _commands.Add(command.Info.Name, command);
                Debug("Loaded command: {CommandName}", command.Info.Name);
            }
        }

        Information("{CommandCount} commands were dynamically loaded", _commands.Count);
    }

    private static ValueTask OnMessage(Privmsg arg)
    {
        ReadOnlySpan<char> content = arg.Content;
        if (ChannelsById[arg.Channel.Id].Priority >= 50 && content.Length > Config.Prefix.Length + 1
            && content.StartsWith(Config.Prefix, StringComparison.CurrentCulture))
        {
            return HandleCommand(arg);
        }

        return default;
    }

    private static ValueTask HandleCommand(Privmsg message)
    {
        ReadOnlySpan<char> content = message.Content;
        foreach (KeyValuePair<string, IChatCommand> kvp in _commands)
        {
            ReadOnlySpan<char> key = kvp.Key;
            if (content[Config.Prefix.Length..].StartsWith(key) && message.Permits(kvp.Value) && !message.IsOnCooldown(kvp.Value))
                return kvp.Value.Run(message);
        }

        return default;
    }
}