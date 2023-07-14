using Bot.Workflows;
using CachingFramework.Redis.Contracts;
using Serilog.Events;

namespace Bot.Models;
internal class InMemorySettings
{
    public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    public int LogLevel { get; set; } = 2;
    public required Dictionary<string, bool> EnabledModules { get; set; }
    private static readonly ILogger _logger = ForContext<InMemorySettings>();

    public Task EnableModule(string moduleName)
    {
        this.EnabledModules[moduleName] = true;
        _logger.Debug("Module enabled: {ModuleName}", moduleName);
        return Cache.SetObjectAsync(Config.SettingsKey, this, when: When.Exists);
    }

    public Task DisableModule(string moduleName)
    {
        this.EnabledModules[moduleName] = false;
        _logger.Debug("Module disabled: {ModuleName}", moduleName);
        return Cache.SetObjectAsync(Config.SettingsKey, this, when: When.Exists);
    }

    public Task ChangeLogLevel(int newLogLevel)
    {
        this.LogLevel = newLogLevel;
        _logger.Debug("Log level changed: {OldLogLevel} -> {NewLogLevel}", LoggerSetup.LogSwitch.MinimumLevel, (LogEventLevel)newLogLevel);
        LoggerSetup.LogSwitch.MinimumLevel = (LogEventLevel)newLogLevel;
        return Cache.SetObjectAsync(Config.SettingsKey, this, when: When.Exists);
    }
}
