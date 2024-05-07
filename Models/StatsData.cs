using System.Text.Json.Serialization;

namespace Bot.Models;

public record StatsData(StatsPayload Payload);
public record StatsPayload(PeriodStats StatisticsClosed);
public record PeriodStats([property: JsonPropertyName("48hours")] Statistic[] _48hours);
public record Statistic(float Median, float MovingAvg, int Volume, DateTime Datetime);

public record Data(ListingsPayload Payload);
public record ListingsPayload(Order[] BuyOrders, Order[] SellOrders);
public record Order(int Platinum, int Quantity, ItemInfo Item);

public record ItemInfo(LocalizedName En);

public record LocalizedName(string ItemName);