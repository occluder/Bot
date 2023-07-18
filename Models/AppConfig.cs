namespace Bot.Models;
#pragma warning disable CS8618
public class AppConfig
{
    public string Prefix { get; init; }
    public string Username { get; init; }
    public string Token { get; init; }
    public string ParentToken { get; init; }
    public long ParentId { get; init; }
    public string RelayChannel { get; init; }
    public string RedisAddress { get; init; }
    public string RedisPass { get; init; }
    public string DbConnectionString { get; init; }
    public string WebhookUrl { get; init; }
    public int DefaultLogLevel { get; init; }
    public string SettingsKey { get; init; }
    public IReadOnlyDictionary<string, string> EvalApi { get; init; }
}
#pragma warning restore CS8618