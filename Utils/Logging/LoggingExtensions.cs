using Serilog.Configuration;
using Serilog.Events;

namespace Bot.Utils.Logging;

public static class LoggingExtensions
{
    public static LoggerConfiguration Discord(
        this LoggerSinkConfiguration loggerConfiguration,
        string webhookUrl,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Debug
    ) => loggerConfiguration.Sink(new DiscordSink(webhookUrl, restrictedToMinimumLevel));
}