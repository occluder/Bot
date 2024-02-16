using Bot.Interfaces;
using Bot.Models;
using Bot.Utils;
using MiniTwitch.Common.Extensions;
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

    public static IEnumerable<IChatCommand> GetCommands() => _commands.Values;

    private static void LoadCommands()
    {
        Type interfaceType = typeof(IChatCommand);
        foreach (Type type in interfaceType.Assembly.GetTypes().Where(interfaceType.IsAssignableFrom))
        {
            if (type.IsInterface || type.IsAbstract ||
                Activator.CreateInstance(type) is not IChatCommand command) continue;
            _commands.Add(command.Info.Name, command);
            Debug("Loaded command: {CommandName}", command.Info.Name);
        }

        Information("{CommandCount} commands were dynamically loaded", _commands.Count);
    }

    private static ValueTask OnMessage(Privmsg arg)
    {
        try
        {
            ReadOnlySpan<char> content = arg.Content;
            if (ChannelsById[arg.Channel.Id].Priority >= 50 && content.Length > Config.Prefix.Length + 1 &&
                content.StartsWith(Config.Prefix, StringComparison.CurrentCulture))
                return HandleCommand(arg);

            return default;
        }
        catch (KeyNotFoundException) when (arg.Author.Id == 0 || arg.Channel.Id == 0)
        {
            AnonClient.ReconnectAsync().StepOver();
            MainClient.ReconnectAsync().StepOver();
        }

        return default;
    }

    private static ValueTask HandleCommand(Privmsg message)
    {
        if (message.Author.Id == Config.Ids["BotId"])
            return default;

        ReadOnlySpan<char> content = message.Content;
        foreach (KeyValuePair<string, IChatCommand> kvp in _commands)
        {
            ReadOnlySpan<char> key = kvp.Key;
            if (content[Config.Prefix.Length..].StartsWith(key) && message.Permits(kvp.Value) &&
                !message.IsOnCooldown(kvp.Value))
            {
                Verbose("{User} running command: {Command}", message.Author.Name, kvp.Value.Info.Name);
                return kvp.Value is ChatCommand chatCommand ? chatCommand.ArgExec(message) : kvp.Value.Run(message);
            }
        }

        return default;
    }
}