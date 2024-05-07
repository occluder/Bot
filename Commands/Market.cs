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
        OneOf<ItemMarket, HttpStatusCode, Exception> response =
            await GetFromRequest<ItemMarket>($"https://api.warframe.market/v1/items/{item}/orders?platform=pc");

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

        OneOf<StatsData, HttpStatusCode, Exception> statsResponse =
            await GetFromRequest<StatsData>($"https://api.warframe.market/v1/items/{item}/statistics");

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

        ItemOrder[] orders = marketInfo.payload.orders;
        ItemOrder[] relevant = [.. orders.Where(
            o => o is { visible: true, order_type: "sell", user.status: "ingame" }
        ).OrderBy(o => o.platinum)];
        Verbose("orders all tidy");
        int startFrom = 0;
        uint last = 0;
        for (int i = 0; i < relevant.Length && i < 5; i++)
        {
            if (last >= 5 && relevant[i].platinum >= last * 2)
            {
                startFrom = i;
                break;
            }

            last = relevant[i].platinum;
        }

        Verbose("startFrom: {Start}", startFrom);
        int volumes = stats.Payload.StatisticsClosed._48hours.Sum(x => x.Volume);
        Verbose("sold last 48h: {Volume}", volumes);
        last = (uint)(relevant.Length > RELEVANT ? RELEVANT : relevant.Length - 1);
        await message.ReplyWith($"pajaBusiness " +
                                $"Price Range: {relevant[0].platinum}-{relevant[last].platinum}P, " +
                                $"Sold last 48h: {volumes}, " +
                                $"Lowest: {relevant[0].platinum}P " +
                                $"https://warframe.market/items/{item}");
    }
}