using Bot.Enums;
using Bot.Interfaces;


namespace Bot.Workflows;

internal class LoadModules : IWorkflow
{
    private readonly HashSet<IModule> _modules = new();

    public async ValueTask<WorkflowState> Run()
    {
        Type interfaceType = typeof(IModule);
        foreach (Type type in interfaceType.Assembly.GetTypes().Where(t => interfaceType.IsAssignableFrom(t) && !t.IsInterface))
        {
            if (Activator.CreateInstance(type) is IModule module)
            {
                _ = _modules.Add(module);
                Debug("Loaded module: {ModuleName}", module.GetType().Name);
                if (Settings.EnabledModules.TryGetValue(module.GetType().Name, out bool enabled) && !enabled)
                    continue;

                await module.Enable();
            }
        }

        Information("{CommandCount} modules were dynamically loaded", _modules.Count);
        return await VerifyModulesPresence();
    }

    private async ValueTask<WorkflowState> VerifyModulesPresence()
    {
        foreach (IModule module in _modules)
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