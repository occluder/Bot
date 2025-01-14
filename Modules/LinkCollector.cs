using System.Text.RegularExpressions;
using Bot.Models;
using Bot.Utils;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class LinkCollector: BotModule
{
    const int MAX_LINKS = 100;
    static readonly Regex _regex = new(
        @"https:[\\/][\\/](www\.|[-a-zA-Z0-9]+\.)?[-a-zA-Z0-9@:%._\+~#=]{3,}(\.[a-zA-Z]{2,10})+(/([-a-zA-Z0-9@:%._\+~#=/?&]+)?)?\b",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50)
    );
    static readonly ILogger _logger = ForContext<LinkCollector>();
    static readonly List<LinkData> _links = new(MAX_LINKS);
    static readonly long[] _knownBots =
    [
        69861108, 100135110, 237719657, 1564983, 513205789, 254941918,
        82008718, 754201843, 19264788, 68136884, 122770725, 62541963
    ];
    readonly BackgroundTimer _timer;

    public LinkCollector()
    {
        _timer = new(TimeSpan.FromMinutes(5), Commit);
    }

    private async ValueTask OnMessage(Privmsg arg)
    {
        if (!ChannelsById[arg.Channel.Id].IsLogged || arg.Content.Length < 10)
        {
            return;
        }

        if (_knownBots.AsSpan().Contains(arg.Author.Id))
        {
            return;
        }

        try
        {
            if (_regex.Match(arg.Content) is { Success: true, Length: > 10 } match && match.Value[0] == 'h')
            {
                if (_links.Count >= MAX_LINKS)
                    await Commit();

                LinkData link = new(
                    arg.Author.Name,
                    arg.Author.Id,
                    arg.Channel.Name,
                    arg.Channel.Id,
                    match.Value,
                    Unix()
                );

                _links.Add(link);
                _logger.Verbose("Link added: {@LinkInfo} ({Total})", link, _links.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "{ClassName} Exception caught");
        }
    }

    private async Task Commit()
    {
        if (!this.Enabled)
        {
            return;
        }

        using var conn = await NewDbConnection();
        try
        {
            int inserted = await conn.ExecuteAsync(
                "insert into chat_links values " +
                "(@Username, @UserId, @Channel, @ChannelId, @LinkText, @TimeSent)",
                _links.ToArray()
            );

            _links.Clear();
            _logger.Debug("Inserted {LinkCount} links", inserted);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to insert links into table");
        }
    }

    protected override ValueTask OnModuleEnabled()
    {
        MainClient.OnMessage += OnMessage;
        AnonClient.OnMessage += OnMessage;
        _timer.Start();
        return default;
    }

    protected override async ValueTask OnModuleDisabled()
    {
        MainClient.OnMessage -= OnMessage;
        AnonClient.OnMessage -= OnMessage;
        await _timer.StopAsync();
    }

    private record LinkData(
        string Username,
        long UserId,
        string Channel,
        long ChannelId,
        string LinkText,
        long TimeSent
    );
}