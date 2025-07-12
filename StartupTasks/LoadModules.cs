using Bot.Enums;
using Bot.Handlers;
using Bot.Interfaces;
using Bot.Models;
using Bot.Modules;

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
                if (Settings.EnabledModules.TryGetValue(module.GetType().Name, out bool enabled))
                {
                    // Exists but not enabled
                    if (!enabled)
                    {
                        continue;
                    }

                    // Enabled
                    await module.Enable();
                    continue;
                }

                // Doesn't exist
                switch (module)
                {
                    // I don't like these
                    case StreamMonitor or Fish:
                        continue;
                    default:
                        await module.Enable();
                        break;
                }
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
            string moduleName = module.GetType().Name;
            if (!Settings.EnabledModules.ContainsKey(moduleName))
                continue;

            await Settings.EnableModule(moduleName);
            Information("Loaded new module: {ModuleName}", moduleName);
        }

        return StartupTaskState.Completed;
    }
}