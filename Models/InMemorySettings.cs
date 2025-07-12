using Bot.StartupTasks;
using Serilog.Events;

namespace Bot.Models;
internal class InMemorySettings
{
    public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    public int LogLevel { get; set; } = 2;
    public Dictionary<string, bool> EnabledModules { get; set; } = [];
    private static readonly ILogger _logger = ForContext<InMemorySettings>();

    public Task EnableModule(string moduleName)
    {
        this.EnabledModules[moduleName] = true;
        _logger.Debug("Module enabled: {ModuleName}", moduleName);
        return PropagateUpdate();
    }

    public Task DisableModule(string moduleName)
    {
        this.EnabledModules[moduleName] = false;
        _logger.Debug("Module disabled: {ModuleName}", moduleName);
        return PropagateUpdate();
    }

    public Task ChangeLogLevel(int newLogLevel)
    {
        this.LogLevel = newLogLevel;
        _logger.Debug("Log level changed: {OldLogLevel} -> {NewLogLevel}", LoggerSetup.LogSwitch.MinimumLevel, (LogEventLevel)newLogLevel);
        LoggerSetup.LogSwitch.MinimumLevel = (LogEventLevel)newLogLevel;
        return PropagateUpdate();
    }

    async Task PropagateUpdate()
    {
        using var db = await NewDbConnection();
        await db.ExecuteAsync("""
            UPDATE persistent_object
            SET value = @Value::jsonb
            WHERE key = @Key
        """, new
        {
            Key = Config.SettingsKey,
            Value = this
        });
    }
}
