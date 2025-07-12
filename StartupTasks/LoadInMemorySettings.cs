using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;
using Serilog.Events;

namespace Bot.StartupTasks;

internal class LoadInMemorySettings: IStartupTask
{
    public static InMemorySettings Settings { get; private set; } = default!;

    public async ValueTask<StartupTaskState> Run()
    {
        try
        {
            using var db = await NewDbConnection();
            var res = await db.QuerySingleOrDefaultAsync<InMemorySettings>("""
                SELECT value FROM persistent_object
                WHERE key = @key
            """, new
            {
                Key = Config.SettingsKey
            }
            );

            if (res is null)
            {
                Settings = new();
                await db.ExecuteAsync(
                    """
                    INSERT INTO
                        persistent_object
                    VALUES
                        (@Key, @Value::jsonb)
                    """, new
                    {
                        Key = Config.SettingsKey,
                        Value = Settings
                    }
                );
            }
            else
            {
                Settings = res;
            }
        }
        catch (Exception ex)
        {
            ForContext<LoadInMemorySettings>().Fatal(ex, "[{ClassName}] Failed to load app settings");
            return StartupTaskState.Failed;
        }

        ForContext<LoadInMemorySettings>().Information("[{ClassName}] Loaded app settings");
        LoggerSetup.LogSwitch.MinimumLevel = (LogEventLevel)Settings.LogLevel;
        return StartupTaskState.Completed;
    }
}