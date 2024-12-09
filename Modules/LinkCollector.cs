using System.Text.RegularExpressions;
using Bot.Models;
using Bot.Utils;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class LinkCollector: BotModule
{
    private const int MAX_LINKS = 500;

    private static readonly Regex _regex = new(
        @"https:[\\/][\\/](www\.|[-a-zA-Z0-9]+\.)?[-a-zA-Z0-9@:%._\+~#=]{3,}(\.[a-zA-Z]{2,10})+(/([-a-zA-Z0-9@:%._\+~#=/?&]+)?)?\b",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly ILogger _logger = ForContext<LinkCollector>();
    private static readonly List<LinkData> _links = new(MAX_LINKS);
    private static readonly SemaphoreSlim _ss = new(1);
    private readonly BackgroundTimer _timer;

    public LinkCollector()
    {
        _timer = new(TimeSpan.FromMinutes(5), Commit, LiveConnectionLock);
    }

    private async ValueTask OnMessage(Privmsg arg)
    {
        if (!ChannelsById[arg.Channel.Id].IsLogged || arg.Content.Length < 10)
        {
            return;
        }

        try
        {
            if (_regex.Match(arg.Content) is { Success: true, Length: > 10 } match && match.Value[0] == 'h')
            {
                if (_links.Count >= MAX_LINKS)
                    await Commit();

                await _ss.WaitAsync();
                LinkData link = new(
                    arg.Author.Name,
                    arg.Author.Id,
                    arg.Channel.Name,
                    arg.Channel.Id,
                    match.Value,
                    Unix()
                );

                _links.Add(link);
                _ = _ss.Release();
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
            return;

        await _ss.WaitAsync();
        try
        {
            int inserted = await LiveDbConnection.ExecuteAsync(
                "insert into chat_links values " +
                "(@Username, @UserId, @Channel, @ChannelId, @LinkText, @TimeSent)",
                _links
            );

            _links.Clear();
            _logger.Debug("Inserted {LinkCount} links", inserted);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to insert links into table");
        }
        finally
        {
            _ = _ss.Release();
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