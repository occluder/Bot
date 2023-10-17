using Bot.Enums;
using Bot.Handlers;
using Bot.Interfaces;
using Bot.Models;

namespace Bot.Workflows;

internal class LoadModules: IWorkflow
{
    public static ModuleHandler Module { get; private set; } = default!;

    public async ValueTask<WorkflowState> Run()
    {
        List<BotModule> modules = new();
        Type abstractType = typeof(BotModule);
        foreach (Type type in abstractType.Assembly.GetTypes().Where(abstractType.IsAssignableFrom))
        {
            if (type.IsClass && !type.IsAbstract && Activator.CreateInstance(type) is BotModule module)
            {
                modules.Add(module);
                Debug("Loaded module: {ModuleName}", module.GetType().Name);
                if (Settings.EnabledModules.TryGetValue(module.GetType().Name, out bool enabled) && !enabled)
                    continue;

                await module.Enable();
            }
        }

        Module = new(modules);
        Information("{CommandCount} modules were dynamically loaded", modules.Count);
        return await VerifyModulesPresence(modules);
    }

    private async ValueTask<WorkflowState> VerifyModulesPresence(List<BotModule> modules)
    {
        foreach (BotModule module in modules)
        {
            string moduleName = module.GetType().Name;
            if (Settings.EnabledModules.ContainsKey(moduleName))
                continue;

            await Settings.EnableModule(moduleName);
            Information("Loaded new module: {ModuleName}", moduleName);
        }

        return WorkflowState.Completed;
    }
}