using System.Text.Json.Serialization;

namespace Bot.Models;

public readonly record struct IvrUser
{
    [JsonPropertyName("banned")]
    public bool Banned { get; init; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; }

    [JsonPropertyName("login")]
    public string Login { get; init; }

    [JsonPropertyName("id")]
    public string Id { get; init; }

    [JsonPropertyName("bio")]
    public string Bio { get; init; }

    [JsonPropertyName("follows")]
    public object Follows { get; init; }

    [JsonPropertyName("followers")]
    public int Followers { get; init; }

    [JsonPropertyName("profileViewCount")]
    public object ProfileViewCount { get; init; }

    [JsonPropertyName("panelCount")]
    public int PanelCount { get; init; }

    [JsonPropertyName("chatColor")]
    public string ChatColor { get; init; }

    [JsonPropertyName("logo")]
    public string Logo { get; init; }

    [JsonPropertyName("banner")]
    public object Banner { get; init; }

    [JsonPropertyName("verifiedBot")]
    public object VerifiedBot { get; init; }

    [JsonPropertyName("emotePrefix")]
    public string EmotePrefix { get; init; }
    [JsonPropertyName("stream")]
    public object Stream { get; init; }
}

