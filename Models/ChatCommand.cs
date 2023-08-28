using Bot.Interfaces;
using MiniTwitch.Irc.Models;

namespace Bot.Models;

public abstract class ChatCommand: IChatCommand
{
    private readonly List<CommandArgument> _userArgs = new(2);
    private readonly Dictionary<string, object> _parsedArgs = new(2);
    private string[] _messageArgs = Array.Empty<string>();

    protected void AddArgument(CommandArgument argument) => _userArgs.Add(argument);

    protected async ValueTask<bool> CheckArguments(Privmsg message)
    {
        _messageArgs = message.Content.Split(' ');
        foreach (CommandArgument arg in _userArgs)
        {
            if (arg.Index > _messageArgs.Length - 1)
            {
                if (arg.Optional)
                    break;

                await message.ReplyWith($"Argument {arg.Index} missing: {arg.Name} ({arg.OutType.Name})");
                return false;
            }

            bool argParse = arg.OutType.Name switch
            {
                "Int32" => Parse<int>(ref message, arg),
                "Int64" => Parse<long>(ref message, arg),
                "Single" => Parse<float>(ref message, arg),
                "Boolean" => ParseBool(ref message, arg),
                "String" => AddString(arg),
                _ => throw new NotSupportedException(
                    $"The type {arg.OutType.Name} is not support for command arguments")
            };

            if (argParse)
                continue;

            await message.ReplyWith($"Argument {arg.Index} error: {arg.Name} must be of type {arg.OutType.Name}");
            return false;
        }

        return true;
    }

    private bool Parse<T>(ref Privmsg message, CommandArgument arg) where T : ISpanParsable<T>
    {
        if (!T.TryParse(_messageArgs[arg.Index], null, out T? value) && !arg.Optional)
            return false;
        if (value is not null)
            _parsedArgs[arg.Name] = value!;

        return true;
    }

    private bool ParseBool(ref Privmsg message, CommandArgument arg)
    {
        if (!bool.TryParse(_messageArgs[arg.Index], out bool value) && !arg.Optional)
            return false;

        _parsedArgs[arg.Name] = value;
        return true;
    }

    private bool AddString(CommandArgument arg)
    {
        if (arg.Index > _messageArgs.Length && arg.Optional)
            return false;

        _parsedArgs[arg.Name] = _messageArgs[arg.Index];
        return true;
    }

    protected T GetArgument<T>(string name) => (T)_parsedArgs[name];

    protected bool TryGetArgument<T>(string name, out T value)
    {
        if (_parsedArgs.TryGetValue(name, out object? val))
        {
            value = (T)val;
            return true;
        }

        value = default!;
        return false;
    }

    public async ValueTask ArgExec(Privmsg message)
    {
        if (!await CheckArguments(message))
            return;

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

    protected record CommandArgument(string Name, uint Index, Type OutType, bool Optional = false);

    private readonly struct TypeHash
    {
        public int Value => HashCode.Combine(_t.FullName, _t.Assembly.FullName);

        private readonly Type _t;

        private TypeHash(Type t)
        {
            _t = t;
        }

        public static implicit operator TypeHash(Type t) => new(t);
    }
}