using System.Net;
using System.Text;
using System.Text.Json;
using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class Market: ChatCommand
{
    private const int RELEVANT = 5;

    public override CommandInfo Info { get; } = new(
        "market",
        "Get an item's platinum price from warframe.market",
        TimeSpan.FromSeconds(5),
        CommandPermission.Everyone
    );

    public override async ValueTask Run(Privmsg message)
    {
        string item = string.Join("_", message.Content.ToLower().Split(' ')[1..]);
        var response = await GetFromRequest<ItemMarket>($"https://api.warframe.market/v2/orders/item/{item}");
        Verbose("Got response for {Item}", item);
        if (!response.TryPickT0(out ItemMarket? marketInfo, out OneOf<HttpStatusCode, Exception> error))
        {
            Warning("Error from https://api.warframe.market/v2/orders/item/{Item}", item);
            await error.Match(
                statusCode => message.ReplyWith($"Received bad status code from warframe.market {statusCode} :("),
                exception =>
                {
                    if (exception is JsonException)
                    {
                        return message.ReplyWith("Item not found :/");
                    }

                    return message.ReplyWith($"Error handling code: ({exception.GetType().Name}) {exception.Message}");
                }
            );

            return;
        }

        var statsResponse = await GetFromRequest<StatsData>($"https://api.warframe.market/v1/items/{item}/statistics");
        Verbose("Got response for {Item}", item);
        if (!statsResponse.TryPickT0(out StatsData? stats, out OneOf<HttpStatusCode, Exception> error2))
        {
            Warning("Error from https://api.warframe.market/v1/items/{item}/statistics", item);
            await error2.Match(
                statusCode => message.ReplyWith($"Received bad status code from warframe.market {statusCode} :("),
                exception => message.ReplyWith($"Error handling code: ({exception.GetType().Name}) {exception.Message}")
            );

            return;
        }

        Verbose("Got {Count} stat entries", stats.Payload.StatisticsClosed._48hours.Length);
        ItemOrder min = marketInfo.data
            .Where(x => x is { order_type: "sell", user.status: "ingame" })
            .MinBy(x => x.platinum)!;

        Verbose("Min order: {MinOrder}", min);

        await message.ReplyWith(
            $"{GetPeriodString(stats.Payload.StatisticsClosed)}, " +
            $"Lowest: {min.platinum}P " +
            $"https://warframe.market/items/{item}"
        );
    }

    private static string GetPeriodString(PeriodStats stats)
    {
        Verbose("Getting period string");
        StringBuilder sb = new();
        int volumes = stats._48hours.Sum(x => x.Volume);
        Func<Statistic, Statistic, float> calcChange = (x, y) => (1 - (x.MovingAvg / y.MovingAvg)) * -100;
        if (stats._90days.All(x => x.ModRank is not null))
        {
            int maxRank = stats._90days.MaxBy(x => x.ModRank)?.ModRank ?? 3;
            int volumesMax = stats._48hours.Where(x => x.ModRank == maxRank).Sum(x => x.Volume);
            Statistic mostRecentR0 = stats._48hours
                .Where(o => o.ModRank == 0)
                .MaxBy(x => x.Datetime)!;

            Statistic mostRecentMax = stats._48hours
                .Where(o => o.ModRank == maxRank)
                .MaxBy(x => x.Datetime)!;

            Statistic? monthAgoR0 = stats._90days
                .Where(o => o.ModRank == 0)
                .Where(o => o.Datetime <= mostRecentR0.Datetime.AddDays(-30))
                .MaxBy(x => x.Datetime);

            Statistic? monthAgoMax = stats._90days
                .Where(o => o.ModRank == maxRank)
                .Where(o => o.Datetime <= mostRecentR0.Datetime.AddDays(-30))
                .MaxBy(x => x.Datetime);

            sb.Append("Avg: (R0) ");
            if (monthAgoR0 is not null)
            {
                float changeR0 = calcChange(mostRecentR0, monthAgoR0);
                sb.Append($"{monthAgoR0.MovingAvg:0.#}P→{mostRecentR0.MovingAvg:0.#}P ({changeR0:+0.##;-0.##}%)");
            }
            else
            {
                sb.Append(mostRecentR0.MovingAvg > 0 ? $"{mostRecentR0.MovingAvg:0.##}P" : "N/A");
            }

            sb.Append($" | (R{maxRank}) ");
            if (monthAgoMax is not null)
            {
                float changeMax = calcChange(mostRecentMax, monthAgoMax);
                sb.Append($"{monthAgoMax.MovingAvg:0.#}P→{mostRecentMax.MovingAvg:0.#}P ({changeMax:+0.##;-0.##}%)");
            }
            else
            {
                sb.Append(mostRecentMax.MovingAvg > 0 ? $"{mostRecentMax.MovingAvg:0.##}P" : "N/A");
            }

            sb.Append(", ");
            sb.Append($"Recently sold: (R0) {volumes - volumesMax} | (R{maxRank}) {volumesMax}");
            Verbose("Got period string");
            return sb.ToString();
        }

        Statistic mostRecent = stats._48hours.MaxBy(x => x.Datetime)!;
        Statistic? monthAgo = stats._90days
            .Where(o => o.Datetime <= mostRecent.Datetime.AddDays(-30))
            .MaxBy(x => x.Datetime);

        sb.Append("Avg: ");
        if (monthAgo is not null)
        {
            sb.Append($"{monthAgo.MovingAvg:0.#}P→{mostRecent.MovingAvg:0.#}P");
            sb.Append($" ({calcChange(mostRecent, monthAgo):+0.##;-0.##}%),");
        }
        else
        {
            sb.Append(mostRecent.MovingAvg > 0 ? $"{mostRecent.MovingAvg:0.##}P" : "N/A");
            sb.Append(", ");
        }
        sb.Append($"Recently sold: {volumes}");
        Verbose("Got period string");
        return sb.ToString();
    }
}