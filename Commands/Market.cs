using System.Net;
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
        var response = await GetFromRequest<ItemMarket>($"https://api.warframe.market/v1/items/{item}/orders?platform=pc");
        Verbose("Got response for {Item}", item);
        if (!response.TryPickT0(out ItemMarket? marketInfo, out OneOf<HttpStatusCode, Exception> error))
        {
            Warning("Error from https://api.warframe.market/v1/items/{Item}/orders?platform=pc", item);
            await error.Match(
                statusCode => message.ReplyWith($"Received bad status code from warframe.market {statusCode} :("),
                exception => message.ReplyWith($"Error handling code: ({exception.GetType().Name}) {exception.Message}")
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
        ItemOrder min = marketInfo.payload.orders.MinBy(x => x.platinum)!;
        int volumes = stats.Payload.StatisticsClosed._48hours.Sum(x => x.Volume);
        Statistic mostRecent = stats.Payload.StatisticsClosed._48hours.MaxBy(x => x.Datetime)!;
        Statistic? weekAgo = stats.Payload.StatisticsClosed._90days
            .Where(o => o.Datetime <= mostRecent.Datetime.AddDays(-7))
            .MaxBy(x => x.Datetime);

        string? changeStr = weekAgo is null 
            ? null 
            : $"({(1 - (mostRecent.MovingAvg / weekAgo.MovingAvg)) * -100:+#.##;-#.##}% this week)";

        await message.ReplyWith($"pajaBusiness " +
                                $"Avg: {mostRecent.MovingAvg:0.#}P {changeStr}, " +
                                $"Sold recently: {volumes}, " +
                                $"Lowest: {min.platinum}P " +
                                $"https://warframe.market/items/{item}");
    }
}