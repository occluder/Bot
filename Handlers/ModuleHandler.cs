using Bot.Models;

namespace Bot.Handlers;

public class ModuleHandler
{
    private static readonly ILogger _logger = ForContext<ModuleHandler>();
    private readonly Dictionary<string, BotModule> _modules;

    public ModuleHandler(IEnumerable<BotModule> modules)
    {
        _modules = modules.ToDictionary(module => module.GetType().Name, module => module);
    }

    public string GetAllModules() => string.Join(", ", _modules.Keys);

    public bool Exists(string name) => _modules.ContainsKey(name);

    public bool IsEnabled(string name) => _modules[name].Enabled;

    public async ValueTask<bool> EnableModule(string name)
    {
        if (!_modules.ContainsKey(name))
        {
            _logger.Error("Cannot enable module {ModuleName} because it does not exist", name);
            return false;
        }
        else if (_modules[name].Enabled)
        {
            _logger.Error("Cannot enable module {ModuleName} because it is already enabled", name);
            return false;
        }

        try
        {
            await _modules[name].Enable();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to enable module {ModuleName}", name);
            return false;
        }

        return true;
    }

    public async ValueTask<bool> DisableModule(string name)
    {
        if (!_modules.ContainsKey(name))
        {
            _logger.Error("Cannot disable module {ModuleName} because it does not exist", name);
            return false;
        }
        else if (!_modules[name].Enabled)
        {
            _logger.Error("Cannot disable module {ModuleName} because it is already disabled", name);
            return false;
        }

        try
        {
            await _modules[name].Disable();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to disable module {ModuleName}", name);
            return false;
        }

        return true;
    }
}