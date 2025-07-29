using System.Collections.Concurrent;
using System.Text;
using Bot.StartupTasks;
using Discord.Webhook;
using Serilog.Core;
using Serilog.Events;

namespace Bot.Utils.Logging;

public class DiscordSink: ILogEventSink
{
    private readonly ILogger _logger = ForContext("ShouldLogToDiscord", false).ForContext<DiscordSink>();
    private readonly ConcurrentQueue<WebhookObject> _logQueue = new();
    private readonly HttpClient _client = new();
    private readonly Webhook _webhook;
    private readonly LogEventLevel _logLevel;
    private readonly Task _caller;

    public DiscordSink(string webhookUrl, LogEventLevel restrictedToMinimumLevel)
    {
        _webhook = new(webhookUrl);
        _logLevel = restrictedToMinimumLevel;
        _caller = Task.Factory.StartNew(async () =>
        {
            WebhookObject aggregateObject = new();
            while (true)
            {
                if (_logQueue.IsEmpty && aggregateObject.embeds.Count == 0)
                {
                    await _webhook.SendAsync(aggregateObject);
                    aggregateObject = new WebhookObject();
                    goto DelayedContinue;
                }

                if (!_logQueue.TryDequeue(out WebhookObject? webhookObject) || webhookObject is null)
                {
                    goto DelayedContinue;
                }

                if (!string.IsNullOrEmpty(webhookObject.content))
                {
                    await _webhook.SendAsync(webhookObject);
                    goto DelayedContinue;
                }

                if (aggregateObject.embeds.Count == 25)
                {
                    await _webhook.SendAsync(aggregateObject);
                    aggregateObject = new WebhookObject();
                }

                aggregateObject.embeds.AddRange(webhookObject.embeds);
                continue;

            DelayedContinue:
                await Task.Delay(2000);
            }
        }, TaskCreationOptions.LongRunning);

        GC.KeepAlive(_caller);
    }

    public void Emit(LogEvent logEvent)
    {
        if (!ShouldLogEvent(logEvent))
        {
            return;
        }

        _logQueue.Enqueue(logEvent.Exception is not null ? CreatExceptionLogObject(logEvent) : CreateLogObject(logEvent));
    }

    static WebhookObject CreatExceptionLogObject(LogEvent log)
    {
        var (title, color) = GetEmbedData(log.Level);
        return new WebhookObject().AddEmbed(builder =>
        {
            builder.WithTitle(title)
            .WithColor(color)
            .WithDescription($"`{log.Exception!.GetType().Name}:` {log.RenderMessage()}")
            .AddField("Message:", FormatExceptionMessage(log.Exception!.Message))
            .AddField("StackTrace:", FormatExceptionMessage(log.Exception.StackTrace ?? string.Empty))
            .AddField("Properties:", FormatProperties(log));
        });
    }

    public WebhookObject CreateLogObject(LogEvent log)
    {
        var (title, color) = GetEmbedData(log.Level);
        return new WebhookObject().AddEmbed(builder =>
        {
            builder.WithTitle(title)
            .WithColor(color)
            .WithDescription(log.RenderMessage())
            .AddField("Properties:", FormatProperties(log));
        });
    }

    private static string FormatExceptionMessage(string message)
    {
        if (message.Length > 900)
            message = message[..900] + " ...";
        if (!string.IsNullOrWhiteSpace(message))
            message = $"```{message}```";

        return message;
    }

    private static string FormatProperties(LogEvent logEvent)
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine("```yaml");
        foreach (KeyValuePair<string, LogEventPropertyValue> entry in logEvent.Properties)
        {
            _ = sb.AppendLine($"{entry.Key}: {entry.Value}");
        }

        _ = sb.AppendLine("```");
        return sb.ToString();
    }

    static readonly DColor _verboseColor = new(215, 215, 215);
    static readonly DColor _debugColor = new(185, 160, 215);
    static readonly DColor _infoColor = new(125, 225, 125);
    static readonly DColor _warningColor = new(235, 250, 10);
    static readonly DColor _errorColor = new(255, 0, 0);
    static readonly DColor _fatalColor = new(60, 0, 15);

    private static (string title, DColor color) GetEmbedData(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => ("📢 Verbose", _verboseColor),
        LogEventLevel.Debug => ("🔍 Debug", _debugColor),
        LogEventLevel.Information => ("ℹ Information", _infoColor),
        LogEventLevel.Warning => ("⚠ Warning", _warningColor),
        LogEventLevel.Error => ("❌ Error", _errorColor),
        LogEventLevel.Fatal => ("💥 Fatal", _fatalColor),
        _ => default
    };

    private bool ShouldLogEvent(LogEvent logEvent) =>
        logEvent.Level >= LoggerSetup.LogSwitch.MinimumLevel &&
        (!logEvent.Properties.ContainsKey("ShouldLogToDiscord") ||
         bool.Parse(logEvent.Properties["ShouldLogToDiscord"].ToString()));
}