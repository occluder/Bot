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

    private long _activation;
    private long _expiry;
    private bool _isDay;

    public override async ValueTask Run(Privmsg message)
    {
        const string sun = "\u2600\ufe0f";
        const string moon = "\ud83c\udf19";
        if (Unix() < _expiry)
        {
            var startedSince = TimeSpan.FromSeconds(Unix() - _activation);
            var expiresIn = TimeSpan.FromSeconds(_expiry - Unix());
            await message.ReplyWith($"{(_isDay ? sun : moon)} since {PrettyTimeString(startedSince)} -> " +
                                    $"{(!_isDay ? sun : moon)} in {PrettyTimeString(expiresIn)}");
            return;
        }

        OneOf<CetusCycle, HttpStatusCode, Exception> response =
            await GetFromRequest<CetusCycle>("https://api.warframestat.us/pc/cetusCycle");

        Verbose("Fetched new cetus cycle");
        await response.Match(cycle =>
            {
                _isDay = cycle.isDay;
                _activation = cycle.activation.ToUnixTimeSeconds();
                _expiry = cycle.expiry.ToUnixTimeSeconds();
                return Run(message);
            },
            statusCode => message.ReplyWith($"Received bad status code: {statusCode} :("),
            exception => message.ReplyWith($"Exception handling code: ({exception.GetType().Name}) {exception.Message}")
        );
    }
}