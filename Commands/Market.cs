using System.Net;
using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class Market: ChatCommand
{
    public Market()
    {
    }

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

        ItemOrder[] orders = marketInfo.payload.orders;
        ItemOrder[] relevant = orders.Where(
            o => o is { visible: true, order_type: "sell" } && (DateTime.Now - o.user.last_seen).TotalDays < 2
        ).OrderBy(o => o.platinum).ToArray();
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
        double avg = relevant[startFrom..].Take(25).Average(o => o.platinum);
        Verbose("avg: {Avg}", avg);
        last = (uint)(relevant.Length > 25 ? 25 : relevant.Length - 1);
        await message.ReplyWith($"pajaBusiness " +
                                $"Active Price Range: {relevant[0].platinum}-{relevant[last].platinum}P, " +
                                $"Avg: {avg:0.0}P, " +
                                $"Lowest: {relevant[0].platinum}P " +
                                $"https://warframe.market/items/{item}");
    }
}