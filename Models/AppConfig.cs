namespace Bot.Models;
#pragma warning disable CS8618
public class AppConfig
{
    public string SettingsKey { get; set; }
    public string Prefix { get; init; }
    public int DefaultLogLevel { get; init; }
    public string RelayChannel { get; init; }
    public string[] PredictionChannels { get; init; }
    public Dictionary<string, string> Links { get; init; }
    public Dictionary<string, string> Secrets { get; init; }
    public Dictionary<string, long> Ids { get; init; }
}
#pragma warning restore CS8618