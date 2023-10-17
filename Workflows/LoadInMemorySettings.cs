using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;
using Serilog.Events;

namespace Bot.Workflows;

internal class LoadInMemorySettings: IWorkflow
{
    public static InMemorySettings Settings { get; private set; } = default!;

    public async ValueTask<WorkflowState> Run()
    {
        try
        {
            Settings = await Cache.FetchObjectAsync(Config.SettingsKey, () => Task.FromResult(new InMemorySettings()
            {
                EnabledModules = new()
            }));
        }
        catch (Exception ex)
        {
            ForContext<LoadInMemorySettings>().Fatal(ex, "[{ClassName}] Failed to load app settings");
            return WorkflowState.Failed;
        }

        ForContext<LoadInMemorySettings>().Information("[{ClassName}] Loaded app settings");
        LoggerSetup.LogSwitch.MinimumLevel = (LogEventLevel)Settings.LogLevel;
        return WorkflowState.Completed;
    }
}