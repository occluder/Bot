using Bot.Enums;
using Bot.Interfaces;
using Bot.Utils.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Bot.StartupTasks;

public class LoggerSetup: IStartupTask
{
    public static LoggingLevelSwitch LogSwitch { get; } = new((LogEventLevel)Config.DefaultLogLevel);

    public ValueTask<StartupTaskState> Run()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Filter.With<ClassNameFilter>()
            .Enrich.WithClassName()
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:yyyy.MM.dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}",
                levelSwitch: LogSwitch)
            .WriteTo.Discord(Config.Links["Webhook"])
            .CreateLogger();

        return ValueTask.FromResult(StartupTaskState.Completed);
    }
}