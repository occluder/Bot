using Serilog.Core;
using Serilog.Events;

namespace Bot.Utils.Logging;

internal class ClassNameFilter : ILogEventFilter
{
    public static string? ClassName { get; set; }

    public bool IsEnabled(LogEvent logEvent)
    {
        if (ClassName is null)
            return true;

        if (logEvent.Properties.TryGetValue("ClassName", out var propValue))
            return propValue.ToString() == ClassName;

        return false;
    }
}
