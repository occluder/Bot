using Bot.Interfaces;
using OneOf.Types;

namespace Bot.Commands.Console;

public class Reload : IConsoleCommand
{
    public string Name { get; } = "reload";
    private readonly Dictionary<string, IReloadable> _reloadables = new();
    private readonly ILogger _logger = ForContext<Reload>();

    public Reload()
    {
        List<IReloadable> reloadables = new();
        Type interfaceType = typeof(IReloadable);
        foreach (Type type in interfaceType.Assembly.GetTypes().Where(interfaceType.IsAssignableFrom))
        {
            if (!type.IsInterface && Activator.CreateInstance(type) is IReloadable obj)
                reloadables.Add(obj);
        }
    }

    public async ValueTask<OneOf<string, Error<string>>> Execute(string[] args)
    {
        if (args.Length < 2)
            return new Error<string>($"Missing required arguments. Usage: reload <{string.Join('|', _reloadables.Keys)}>");

        if (_reloadables.TryGetValue(args[1], out var reloadable))
        {
            bool result = await reloadable.Reload();
            if (result)
                _logger.Information("Successfully reloaded {ReloadableName}", reloadable.ReloadKey);
            else
                _logger.Warning("Failed to reload {ReloadableName}", reloadable.ReloadKey);

            return result ? $"Successfully reloaded {reloadable.ReloadKey}" : $"Failed to reload {reloadable.ReloadKey}";
        }

        return new Error<string>($"\"{args[1]}\" cannot be reloaded or does not exist");
    }
}
