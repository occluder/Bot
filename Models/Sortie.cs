using System.Text.Json.Serialization;

namespace Bot.Models;

public sealed record Sortie(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("activation")]
    DateTime Activation,
    [property: JsonPropertyName("expiry")] DateTime Expiry,
    [property: JsonPropertyName("startString")]
    string StartString,
    [property: JsonPropertyName("active")] bool Active,
    [property: JsonPropertyName("rewardPool")]
    string RewardPool,
    [property: JsonPropertyName("variants")]
    IReadOnlyList<Variant> Variants,
    [property: JsonPropertyName("boss")] string Boss,
    [property: JsonPropertyName("faction")]
    string Faction,
    [property: JsonPropertyName("factionKey")]
    string FactionKey,
    [property: JsonPropertyName("expired")]
    bool Expired,
    [property: JsonPropertyName("eta")] string Eta
);

public sealed record Variant(
    [property: JsonPropertyName("node")] string Node,
    [property: JsonPropertyName("boss")] string Boss,
    [property: JsonPropertyName("missionType")]
    string MissionType,
    [property: JsonPropertyName("planet")] string Planet,
    [property: JsonPropertyName("modifier")]
    string Modifier,
    [property: JsonPropertyName("modifierDescription")]
    string ModifierDescription
);