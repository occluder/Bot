using Bot.Enums;
using Bot.Handlers;
using Bot.Interfaces;
using Bot.Models;

namespace Bot.StartupTasks;

internal class LoadModules: IStartupTask
{
    public static ModuleHandler Module { get; private set; } = default!;

    public async ValueTask<StartupTaskState> Run()
    {
        List<BotModule> modules = [];
        Type abstractType = typeof(BotModule);
        foreach (Type type in abstractType.Assembly.GetTypes().Where(abstractType.IsAssignableFrom))
        {
            if (type.IsClass && !type.IsAbstract && Activator.CreateInstance(type) is BotModule module)
            {
                modules.Add(module);
                Debug("Loaded module: {ModuleName}", module.GetType().Name);
            }
        }

        Module = new(modules);
        Information("{CommandCount} modules were dynamically loaded", modules.Count);
        return await VerifyModulesPresence(modules);
    }

    private async ValueTask<StartupTaskState> VerifyModulesPresence(List<BotModule> modules)
    {
        foreach (BotModule module in modules)
        {
            if (Settings.EnabledModules.TryGetValue(module.Name, out bool enabled) && enabled)
            {
                await module.Enable();
                await Settings.EnableModule(module.Name);
                Information("Enabled module: {ModuleName}", module.Name);
            }
        }

        return StartupTaskState.Completed;
    }
}