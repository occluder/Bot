global using static Bot.Workflows.LoadModules;
using Bot.Enums;
using Bot.Handlers;
using Bot.Interfaces;


namespace Bot.Workflows;

internal class LoadModules : IWorkflow
{
    public static ModuleHandler Module { get; private set; } = default!;

    public async ValueTask<WorkflowState> Run()
    {
        List<IModule> modules = new();
        Type interfaceType = typeof(IModule);
        foreach (Type type in interfaceType.Assembly.GetTypes().Where(t => interfaceType.IsAssignableFrom(t) && !t.IsInterface))
        {
            if (Activator.CreateInstance(type) is IModule module)
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

    private async ValueTask<WorkflowState> VerifyModulesPresence(List<IModule> modules)
    {
        foreach (IModule module in modules)
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