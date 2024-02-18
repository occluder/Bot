using System.Net;
using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class Market: ChatCommand
{
    public override CommandInfo Info { get; } = new(
        "market",
        "Get an item's platinum price from warframe.market",
        TimeSpan.FromSeconds(5),
        CommandPermission.Everyone
    );

    public Market()
    {
        AddArgument(new CommandArgument("ItemName", 1, typeof(string)));
    }

    public override async ValueTask Run(Privmsg message)
    {
        string item = GetArgument<string>("ItemName").ToLower().Replace(' ', '_');
        OneOf<ItemMarket, HttpStatusCode, Exception> response =
            await GetFromRequest<ItemMarket>($"https://api.warframe.market/v1/items/{item}/orders?platform=pc");

        if (!response.TryPickT0(out ItemMarket? marketInfo, out OneOf<HttpStatusCode, Exception> error))
        {
            await error.Match(
                statusCode => message.ReplyWith($"Received bad status code from warframe.market {statusCode} :("),
                exception => message.ReplyWith($"Error handling code: ({exception.GetType().Name}) {exception.Message}")
            );

            return;
        }

        ItemOrder[] orders = marketInfo.payload.orders;
        ItemOrder[] relevant = orders.Where(
            o => o.visible && (o.user.status != "offline" || (DateTime.Now - o.user.last_seen).TotalDays < 7)
        ).OrderBy(o => o.platinum).ToArray();
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

        double avg = relevant[startFrom..].Take(10).Average(o => o.platinum);
        await message.ReplyWith($"pajaBusiness Active Price Range: {relevant[0].platinum}-{relevant[^1].platinum}P, " +
                                $"Avg: {avg:0.0}P, " +
                                $"Lowest: {relevant[0].platinum}P " +
                                $"https://warframe.market/items/{item}");
    }
}