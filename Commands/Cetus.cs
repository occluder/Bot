using System.Net;
using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class Cetus: ChatCommand
{
    public override CommandInfo Info { get; } = new(
        "cetus",
        "Get the current time in Cetus",
        TimeSpan.FromSeconds(5),
        CommandPermission.Everyone
    );

    public override async ValueTask Run(Privmsg message)
    {
        OneOf<CetusCycle, HttpStatusCode, Exception> response =
            await GetFromRequest<CetusCycle>("https://api.warframestat.us/pc/cetusCycle");

        const string sun = "\u2600\ufe0f";
        const string moon = "\ud83c\udf19";
        Verbose("Fetched new cetus cycle");
        await response.Match(
            cycle => message.ReplyWith($"{(cycle.isDay ? moon : sun)} in {cycle.timeLeft}"),
            statusCode => message.ReplyWith($"Received bad status code: {statusCode} :("),
            exception => message.ReplyWith($"Exception handling code: ({exception.GetType().Name}) {exception.Message}")
        );
    }
}