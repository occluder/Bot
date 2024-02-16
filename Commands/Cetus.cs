using System.Net;
using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class Cetus: ChatCommand
{
    private long _checkAfter;
    private bool _isDay;

    public override CommandInfo Info { get; } = new(
        "cetus",
        string.Empty,
        TimeSpan.FromSeconds(5),
        CommandPermission.Everyone
    );

    public override async ValueTask Run(Privmsg message)
    {
        if (Unix() < _checkAfter)
        {
            Verbose("Cetus cycle is still active. Day: {IsDay}", _isDay);
            var diff = TimeSpan.FromSeconds(_checkAfter - Unix());
            string cycleString = $"{(_isDay ? "a" : "b")} {PrettyTimeString(diff)}";
            Verbose("It is currently {Cycle}", _isDay ? "day" : "night");
            Verbose("Time until new cycle: {Expiry}", PrettyTimeString(diff));
            await message.ReplyWith(cycleString);
            return;
        }

        Verbose("Cetus cycle expired. Fetching new cycle...");
        OneOf<CetusCycle, HttpStatusCode, Exception> response =
            await GetFromRequest<CetusCycle>("https://api.warframestat.us/pc/cetusCycle");

        Verbose("Fetched new cetus cycle");
        await response.Match(cycle =>
            {
                _isDay = cycle.isDay;
                _checkAfter = cycle.expiry.ToUnixTimeSeconds();
                return Run(message);
            },
            statusCode => message.ReplyWith($"Received bad status code: {statusCode} :("),
            exception => message.ReplyWith($"Exception handling code: ({exception.GetType().Name}) {exception.Message}")
        );
    }
}