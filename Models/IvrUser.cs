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

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }

    [JsonPropertyName("emotePrefix")]
    public string EmotePrefix { get; init; }

    [JsonPropertyName("roles")]
    public UserRoles Roles { get; init; }

    [JsonPropertyName("badges")]
    public List<object> Badges { get; init; }

    [JsonPropertyName("chatterCount")]
    public int ChatterCount { get; init; }

    [JsonPropertyName("chatSettings")]
    public UserChatSettings ChatSettings { get; init; }

    [JsonPropertyName("stream")]
    public object Stream { get; init; }

    [JsonPropertyName("lastBroadcast")]
    public UserLastBroadcast LastBroadcast { get; init; }

    [JsonPropertyName("panels")]
    public List<UserPanel> Panels { get; init; }

    public readonly record struct UserChatSettings(
        [property: JsonPropertyName("chatDelayMs")] int ChatDelayMs,
        [property: JsonPropertyName("followersOnlyDurationMinutes")] object FollowersOnlyDurationMinutes,
        [property: JsonPropertyName("slowModeDurationSeconds")] object SlowModeDurationSeconds,
        [property: JsonPropertyName("blockLinks")] bool BlockLinks,
        [property: JsonPropertyName("isSubscribersOnlyModeEnabled")] bool IsSubscribersOnlyModeEnabled,
        [property: JsonPropertyName("isEmoteOnlyModeEnabled")] bool IsEmoteOnlyModeEnabled,
        [property: JsonPropertyName("isFastSubsModeEnabled")] bool IsFastSubsModeEnabled,
        [property: JsonPropertyName("isUniqueChatModeEnabled")] bool IsUniqueChatModeEnabled,
        [property: JsonPropertyName("requireVerifiedAccount")] bool RequireVerifiedAccount,
        [property: JsonPropertyName("rules")] IReadOnlyList<object> Rules
    );
    public readonly record struct UserLastBroadcast(
        [property: JsonPropertyName("startedAt")] DateTime StartedAt,
        [property: JsonPropertyName("title")] string Title
    );
    public readonly record struct UserPanel(
        [property: JsonPropertyName("id")] string Id
    );
    public readonly record struct UserRoles(
        [property: JsonPropertyName("isAffiliate")] bool IsAffiliate,
        [property: JsonPropertyName("isPartner")] bool IsPartner,
        [property: JsonPropertyName("isStaff")] object IsStaff
    );
}

