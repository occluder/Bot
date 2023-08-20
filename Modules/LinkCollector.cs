using System.Text.RegularExpressions;
using Bot.Models;
using Bot.Utils;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class LinkCollector : BotModule
{
    private const string BOTS_REDIS_KEY = "bot:chat:detected_bots";
    private const int MAX_LINKS = 250;

    private static readonly Regex _regex = new(@"https?:[\\/][\\/](www\.|[-a-zA-Z0-9]+\.)?[-a-zA-Z0-9@:%._\+~#=]{3,}(\.[a-zA-Z]{2,10})+(/([-a-zA-Z0-9@:%._\+~#=/?&]+)?)?\b", RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private static readonly ILogger _logger = ForContext<LinkCollector>();
    private static readonly List<LinkData> _links = new(MAX_LINKS);
    private static SemaphoreSlim _ss = new(1);
    private readonly BackgroundTimer _timer;

    public LinkCollector()
    {
        _timer = new(TimeSpan.FromMinutes(5), Commit, PostgresTimerSemaphore);
    }

    private async ValueTask OnMessage(Privmsg arg)
    {
        try
        {
            if (!ChannelsById[arg.Channel.Id].IsLogged || arg.Content.Length < 10 || await IsBot(arg))
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

    private static async ValueTask<bool> IsBot(Privmsg message)
    {
        if (message.Nonce.Length > 0)
            return false;

        if (await Collections.GetRedisSet<long>(BOTS_REDIS_KEY).ContainsAsync(message.Author.Id))
        {
            return true;
        }
        else if (message.Author.Name.EndsWith("bot", StringComparison.CurrentCultureIgnoreCase))
        {
            await Collections.GetRedisSet<long>(BOTS_REDIS_KEY).AddAsync(message.Author.Id);
            _logger.Verbose("New bot detected: {BotUsername}", message.Author.Name);
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