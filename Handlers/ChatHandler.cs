using Bot.Interfaces;
using Bot.Utils;
using MiniTwitch.Irc.Models;
using OneOf.Types;

namespace Bot.Handlers;

public static class ChatHandler
{
    private static Dictionary<string, IConsoleCommand> _consoleCommands = new();
    private static Dictionary<string, IChatCommand> _chatCommands = new();
    private static IConsoleCommand _consoleCommandNotFound = default!;

    public static void Setup()
    {
        MainClient.OnMessage += OnMessage;
        AnonClient.OnMessage += OnMessage;

        LoadCommands();
    }

    private static void LoadCommands()
    {
        Type chatInterfaceType = typeof(IChatCommand);
        foreach (Type type in chatInterfaceType.Assembly.GetTypes().Where(t => chatInterfaceType.IsAssignableFrom(t) && !t.IsInterface))
        {
            if (Activator.CreateInstance(type) is IChatCommand command)
            {
                _chatCommands.Add(command.Info.Name, command);
                Debug("Loaded chat command: {CommandName}", command.Info.Name);
            }
        }

        Information("{CommandCount} chat commands were dynamically loaded", _chatCommands.Count);
        Type consoleInterfaceType = typeof(IChatCommand);
        foreach (Type type in consoleInterfaceType.Assembly.GetTypes().Where(consoleInterfaceType.IsAssignableFrom))
        {
            if (!type.IsInterface && Activator.CreateInstance(type) is IConsoleCommand command)
            {
                if (Guid.TryParse(command.Name, out _))
                    _consoleCommandNotFound = command;
                else
                    _consoleCommands.Add(command.Name, command);

                Debug("Loaded console command: {CommandName}", command.Name);
            }
        }

        Information("{CommandCount} console commands were dynamically loaded", _consoleCommands.Count);
    }

    private static ValueTask OnMessage(Privmsg arg)
    {
        ReadOnlySpan<char> content = arg.Content;
        if (ChannelsById[arg.Channel.Id].Priority >= 50 && content.Length > Config.Prefix.Length + 1
            && content.StartsWith(Config.Prefix, StringComparison.CurrentCulture))
        {
            return HandleTwitchCommand(arg);
        }

        return default;
    }

    private static ValueTask HandleTwitchCommand(Privmsg message)
    {
        ReadOnlySpan<char> content = message.Content;
        foreach (KeyValuePair<string, IChatCommand> kvp in _chatCommands)
        {
            ReadOnlySpan<char> key = kvp.Key;
            if (content[Config.Prefix.Length..].StartsWith(key) && message.Permits(kvp.Value) && !message.IsOnCooldown(kvp.Value))
                return kvp.Value.Run(message);
        }

        return default;
    }

    public static ValueTask<OneOf<string, Error<string>>> HandleConsoleCommand(string input)
    {
        string[] args = input.Split(' ');
        return _consoleCommands.Values.FirstOrDefault(x => x.Name == args[0], _consoleCommandNotFound).Execute(args);
    }
}