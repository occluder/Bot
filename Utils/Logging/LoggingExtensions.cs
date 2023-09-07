using Serilog.Configuration;
using Serilog.Events;

namespace Bot.Utils.Logging;

public static class LoggingExtensions
{
    public static LoggerConfiguration Discord(this LoggerSinkConfiguration loggerConfiguration, string webhookUrl, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Debug,
        LogEventLevel propsRestrictedToMinimumLevel = LogEventLevel.Verbose) => loggerConfiguration.Sink(new DiscordSink(webhookUrl, restrictedToMinimumLevel, propsRestrictedToMinimumLevel));
}