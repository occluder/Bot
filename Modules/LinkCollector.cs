using System.Text.RegularExpressions;
using Bot.Models;
using Bot.Utils;
using CachingFramework.Redis.Contracts.RedisObjects;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class LinkCollector: BotModule
{
    private const int MAX_LINKS = 250;

    private static readonly Regex _regex = new(@"https?:[\\/][\\/](www\.|[-a-zA-Z0-9]+\.)?[-a-zA-Z0-9@:%._\+~#=]{3,}(\.[a-zA-Z]{2,10})+(/([-a-zA-Z0-9@:%._\+~#=/?&]+)?)?\b", RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private static readonly ILogger _logger = ForContext<LinkCollector>();
    private static readonly List<LinkData> _links = new(MAX_LINKS);
    private static readonly SemaphoreSlim _ss = new(1);
    private static readonly IRedisSet<long> _redisBotList = Collections.GetRedisSet<long>("bot:chat:bot_list");
    private readonly HashSet<long> _bots;
    private readonly BackgroundTimer _timer;

    public LinkCollector()
    {
        _timer = new(TimeSpan.FromMinutes(5), Commit, PostgresQueryLock);
        _bots = _redisBotList.ToHashSet();
    }

    private async ValueTask OnMessage(Privmsg arg)
    {
        try
        {
            if (!ChannelsById[arg.Channel.Id].IsLogged
                || arg.Content.Length < 10
                || _bots.Contains(arg.Author.Id)
                || IsBot(arg.Author, arg.Nonce))
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
            await _redisBotList.AddRangeAsync(_bots);
        }
    }

    private bool IsBot(MessageAuthor author, string nonce)
    {
        if (nonce.Length > 0)
            return false;

        ReadOnlySpan<char> nameSpan = author.Name;
        if (!nameSpan.Contains("bot", StringComparison.OrdinalIgnoreCase)) return false;
        if (_bots.Add(author.Id)) _logger.Verbose("New bot detected: {BotUsername}", author.Name);
        return true;
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