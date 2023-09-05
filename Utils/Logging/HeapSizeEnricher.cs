using Serilog.Core;
using Serilog.Events;

namespace Bot.Utils.Logging;
public class HeapSizeEnricher: ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("HeapSizeMB",
            Math.Round(GC.GetTotalMemory(false) / 1000000M, 2)));
    }
}