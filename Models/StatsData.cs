using System.Text.Json.Serialization;

namespace Bot.Models;

public record StatsData([property: JsonPropertyName("payload")] StatsPayload Payload);
public record StatsPayload([property: JsonPropertyName("statistics_closed")] PeriodStats StatisticsClosed);
public record PeriodStats(
    [property: JsonPropertyName("48hours")] Statistic[] _48hours,
    [property: JsonPropertyName("90days")] Statistic[] _90days
);

public record Statistic(
    [property: JsonPropertyName("median")] float Median,
    [property: JsonPropertyName("moving_avg")] float MovingAvg,
    [property: JsonPropertyName("volume")] int Volume,
    [property: JsonPropertyName("datetime")] DateTime Datetime,
    [property: JsonPropertyName("mod_rank")] int? ModRank
);
