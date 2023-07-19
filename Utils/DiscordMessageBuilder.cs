using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bot.Models;

namespace Bot.Utils;

internal class DiscordMessageBuilder
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [JsonPropertyName("content")]
    public string? Content { get; set; }
    [JsonPropertyName("embeds")]
    public List<DiscordEmbed> Embeds { get; set; } = new(10);

    public DiscordMessageBuilder(string? content = null)
    {
        Content = content;
    }

    public DiscordMessageBuilder AddEmbed(Action<DiscordEmbed> embedAction)
    {
        DiscordEmbed embed = new();
        embedAction(embed);
        Embeds.Add(embed);
        return this;
    }

    public StringContent ToStringContent()
    {
        string c = JsonSerializer.Serialize(this, _serializerOptions);
        return new(c, Encoding.UTF8, "application/json");
    }

    public override string ToString() => JsonSerializer.Serialize(this, _serializerOptions);
}
