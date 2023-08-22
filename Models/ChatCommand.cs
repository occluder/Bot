using Bot.Interfaces;
using MiniTwitch.Irc.Models;

namespace Bot.Models;

public abstract class ChatCommand : IChatCommand
{
    private static readonly Type[] _parsables = new[] { typeof(long), typeof(int), typeof(bool) };
    private readonly List<CommandArgument> _userArgs = new(2);
    private readonly Dictionary<string, object> _parsedArgs = new(2);
    private string[] _messageArgs = Array.Empty<string>();

    protected void AddArgument(CommandArgument argument)
    {
        if (!_parsables.Contains(argument.OutType) && argument.OutType != typeof(string))
            throw new NotSupportedException($"The type \"{argument.OutType.Name}\" is not supported for arguments");

        _userArgs.Add(argument);
    }

    protected ValueTask CheckArguments(Privmsg message)
    {
        _messageArgs = message.Content.Split(' ');
        foreach (CommandArgument arg in _userArgs)
        {
            if (arg.Index > _messageArgs.Length - 1)
                if (!arg.Optional)
                    return message.ReplyWith($"Argument {arg.Index} missing: {arg.Name} ({arg.OutType.Name})");
                else
                    break;

            if (arg.OutType == typeof(long))
            {
                if (long.TryParse(_messageArgs[arg.Index], out long @long))
                    _parsedArgs[arg.Name] = @long;
                else
                    return message.ReplyWith($"Argument {arg.Index} error: {arg.Name} must be of type {arg.OutType.Name}");
            }
            else if (arg.OutType == typeof(int))
            {
                if (int.TryParse(_messageArgs[arg.Index], out int @int))
                    _parsedArgs[arg.Name] = @int;
                else
                    return message.ReplyWith($"Argument {arg.Index} error: {arg.Name} must be of type {arg.OutType.Name}");
            }
            else if (arg.OutType == typeof(bool))
            {
                if (bool.TryParse(_messageArgs[arg.Index], out bool @bool))
                    _parsedArgs[arg.Name] = @bool;
                else
                    return message.ReplyWith($"Argument {arg.Index} error: {arg.Name} must be of type {arg.OutType.Name}");
            }
            else
            {
                _parsedArgs[arg.Name] = _messageArgs[arg.Index];
            }
        }

        return default;
    }

    protected T GetArgument<T>(string name) => (T)_parsedArgs[name];

    protected bool TryGetArgument<T>(string name, out T value)
    {
        if (_parsedArgs.TryGetValue(name, out var val))
        {
            value = (T)val;
            return true;
        }

        value = default!;
        return false;
    }

    protected record CommandArgument(string Name, uint Index, Type OutType, bool Optional = false);

    public abstract CommandInfo Info { get; }

    public abstract ValueTask Run(Privmsg message);

}
