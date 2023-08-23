using System.Text.RegularExpressions;
using Bot.Models;
using Bot.Utils;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class LinkCollector : BotModule
{
    private const int MAX_LINKS = 250;

    private static readonly Regex _regex = new(@"https?:[\\/][\\/](www\.|[-a-zA-Z0-9]+\.)?[-a-zA-Z0-9@:%._\+~#=]{3,}(\.[a-zA-Z]{2,10})+(/([-a-zA-Z0-9@:%._\+~#=/?&]+)?)?\b", RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private static readonly ILogger _logger = ForContext<LinkCollector>();
    private static readonly List<LinkData> _links = new(MAX_LINKS);
    private static readonly SemaphoreSlim _ss = new(1);
    private static readonly HashSet<string> _bots = new()
    {
        "streamelements", "streamlabs", "scriptorex", "apulxd", "rewardmore", "iogging", "ttdb", "supibot", "nightbot"
    };
    private readonly BackgroundTimer _timer;

    public LinkCollector()
    {
        _timer = new(TimeSpan.FromMinutes(5), Commit, PostgresQueryLock);
    }

    private async ValueTask OnMessage(Privmsg arg)
    {
        try
        {
            if (!ChannelsById[arg.Channel.Id].IsLogged || arg.Content.Length < 10 || _bots.Contains(arg.Author.Name) || IsBot(arg.Author.Name, arg.Nonce))
                return;

            if (_regex.Match(arg.Content) is { Success: true, Length: > 10 } match && match.Value[0] == 'h')
            {
                if (_links.Count >= MAX_LINKS)
                    await Commit();

                await _ss.WaitAsync();
                LinkData link = new(arg.Author.Name, arg.Channel.Name, match.Value, DateTime.Now);
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
            int inserted = await Postgres.ExecuteAsync("insert into collected_links values (@Username, @Channel, @LinkText, @TimePosted)", _links);
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

    private static bool IsBot(string name, string nonce)
    {
        const string bot = "bot";

        if (nonce.Length > 0)
            return false;

        ReadOnlySpan<char> nameSpan = name;
        if (nameSpan.EndsWith(bot, StringComparison.CurrentCultureIgnoreCase))
        {
            if (_bots.Add(bot))
                _logger.Verbose("New bot detected: {BotUsername}", name);
            return true;
        }

        return false;
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

    private record struct LinkData(string Username, string Channel, string LinkText, DateTime TimePosted)
    {
        public static implicit operator LinkData((string, string, string, DateTime) tuple) =>
            new(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
    };
}