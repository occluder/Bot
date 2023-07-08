using Serilog.Configuration;
using Serilog.Events;

namespace Bot.Utils.Logging;

public static class LoggingExtensions
{
    public static LoggerConfiguration Discord(this LoggerSinkConfiguration loggerConfiguration, string webhookUrl, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Debug,
        LogEventLevel propsRestrictedToMinimumLevel = LogEventLevel.Verbose)
    {
        return loggerConfiguration.Sink(new DiscordSink(webhookUrl, restrictedToMinimumLevel, propsRestrictedToMinimumLevel));
    }

    public static LoggerConfiguration WithHeapSize(this LoggerEnrichmentConfiguration cfg, LogEventLevel maxEnrichmentLevel = LogEventLevel.Debug)
    {
        ArgumentNullException.ThrowIfNull(cfg, nameof(cfg));
        return cfg.With(new HeapSizeEnricher(maxEnrichmentLevel));
    }
}