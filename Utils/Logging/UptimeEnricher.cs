using Serilog.Core;
using Serilog.Events;

namespace Bot.Utils.Logging;

public class UptimeEnricher: ILogEventEnricher
{
    private static readonly DateTime _startTime = DateTime.Now;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) =>
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Uptime", DateTime.Now - _startTime));
}