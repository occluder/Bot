using Bot.Interfaces;
using MiniTwitch.Irc.Models;

namespace Bot.Models;

public abstract class ChatCommand: IChatCommand
{
    readonly List<CommandArgument> _definedArgs = [];
    readonly Dictionary<string, ArgumentInput> _parsedArgs = [];
    string[] _messageArgs = [];

    protected void AddArgument(CommandArgument argument)
    {
        if (argument.TakeRemaining && argument.ArgumentType != typeof(string))
        {
            throw new ArgumentException("Only string arguments can consume remaining input");
        }

        if (_definedArgs.Count > 0)
        {
            if (_definedArgs[^1].Optional && !argument.Optional)
            {
                throw new ArgumentException("Cannot add a required argument after an optional argument");
            }

            if (_definedArgs[^1].TakeRemaining)
            {
                throw new ArgumentException("Cannot add an argument after an argument that consumes remaining input");
            }
        }

        _definedArgs.Add(argument);
    }

    protected async ValueTask<bool> CheckArguments(Privmsg message)
    {
        _messageArgs = message.Content.Split(' ');
        if (_messageArgs.Length - 1 < _definedArgs.Count(arg => !arg.Optional))
        {
            var firstMissingArg = _definedArgs[_messageArgs.Length - 1];
            await message.ReplyWith($"{firstMissingArg.ArgumentType.Name} argument \"{firstMissingArg.Name}\" is missing");
            return false;
        }

        int argIdx = 0;
        foreach (CommandArgument arg in _definedArgs)
        {
            if (arg.TakeRemaining)
            {
                if (argIdx >= _messageArgs.Length)
                {
                    if (!arg.Optional)
                    {
                        await message.ReplyWith($"{arg.ArgumentType.Name} argument \"{arg.Name}\" is missing");
                        return false;
                    }

                    return true;
                }

                string remaining = string.Join(' ', _messageArgs[argIdx..]);
                _parsedArgs[arg.Name] = new(remaining);
                break;
            }

            if (!ParseUserArg(_messageArgs[argIdx++], arg))
            {
                await message.ReplyWith($"Argument {argIdx - 1} error: {arg.Name} must be of type {arg.ArgumentType.Name}");
                return false;
            }
        }

        return true;
    }

    private bool ParseUserArg(string input, CommandArgument defined)
    {
        switch (defined.ArgumentType.Name)
        {
            case nameof(Int32):
                if (int.TryParse(input, out int intValue))
                {
                    _parsedArgs[defined.Name] = new(intValue);
                    return true;
                }
                return false;
            case nameof(Int64):
                if (long.TryParse(input, out long longValue))
                {
                    _parsedArgs[defined.Name] = new(longValue);
                    return true;
                }
                return false;
            case nameof(Single):
                if (float.TryParse(input, out float floatValue))
                {
                    _parsedArgs[defined.Name] = new(floatValue);
                    return true;
                }
                return false;
            case nameof(Boolean):
                if (bool.TryParse(input, out bool boolValue))
                {
                    _parsedArgs[defined.Name] = new(boolValue);
                    return true;
                }
                return false;
            case nameof(String):
                _parsedArgs[defined.Name] = new(input);
                return true;
            default:
                return false;
        }
    }
    protected ArgumentInput GetArgument(string name) => _parsedArgs[name];

    protected bool TryGetArgument(string name, out ArgumentInput? value)
    {
        return _parsedArgs.TryGetValue(name, out value);
    }

    public async ValueTask ArgExec(Privmsg message)
    {
        if (!await CheckArguments(message))
        {
            return;
        }

        try
        {
            await Run(message);
        }
        finally
        {
            _parsedArgs.Clear();
        }
    }

    public abstract CommandInfo Info { get; }
    public abstract ValueTask Run(Privmsg message);

    protected record CommandArgument(string Name, Type ArgumentType, bool Optional = false, bool TakeRemaining = false);
    protected class ArgumentInput: OneOfBase<int, long, float, bool, string>
    {
        public ArgumentInput(OneOf<int, long, float, bool, string> input) : base(input)
        {
        }
        public int AssumedInt => AsT0;
        public long AssumedLong => AsT1;
        public float AssumedFloat => AsT2;
        public bool AssumedBool => AsT3;
        public string AssumedString => AsT4;
    }
}