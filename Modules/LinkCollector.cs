using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Bot.Interfaces;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class LinkCollector : IModule
{
    public bool Enabled { get; }

    private static readonly Regex _regex = new(@"https?:[\\/][\\/](www\.|[-a-zA-Z0-9]+\.)?[-a-zA-Z0-9@:%._\+~#=]{3,}(\.[a-zA-Z]{2,10})+(/([-a-zA-Z0-9@:%._\+~#=/?&]+)?)?\b", RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private static readonly ILogger _logger = ForContext<LinkCollector>();
    private static readonly List<LinkData> _links = new(1500);
    private static SemaphoreSlim _ss = new(1);
    private static readonly HashSet<string> _bots = new()
    {
        "streamelements", "streamlabs", "scriptorex", "apulxd", "rewardmore", "iogging", "ttdb"
    };
    private static int _commitAt = Random.Shared.Next(10, 1000);

    private async ValueTask OnMessage(Privmsg arg)
    {
        try
        {
            if (!ChannelsById[arg.Channel.Id].IsLogged || arg.Content.Length < 10 || _bots.Contains(arg.Author.Name) || IsBot(arg.Author.Name, arg.Nonce))
                return;

            if (_regex.Match(arg.Content) is { Success: true, Length: > 10 } match && match.Value[0] == 'h')
            {
                await _ss.WaitAsync();
                _links.Add(new(arg.Author.Name, arg.Channel.Name, match.Value, DateTime.Now));
                _ = _ss.Release();
            }

            if (_links.Count % _commitAt == 0)
                await Commit();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "{ClassName} Exception caught");
        }
    }

    private static async Task Commit()
    {
        try
        {
            await _ss.WaitAsync();
            int inserted = await Postgres.ExecuteAsync("insert into collected_links values (@Username, @Channel, @LinkText, @TimePosted)", _links);
            _ = _ss.Release();
            _logger.Information("Inserted {LinkCount} links", inserted);
            _commitAt = Random.Shared.Next(25, 1000);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to insert links into table");
        }
    }

    private static bool IsBot(string name, string nonce)
    {
        const string bot = "bot";

        if (nonce.Length > 0)
            return false;

        ReadOnlySpan<char> nameSpan = name;
        if (nameSpan.Contains(bot, StringComparison.CurrentCultureIgnoreCase))
        {
            _ = _bots.Add(bot);
            return true;
        }

        return false;
    }

    public async ValueTask Enable()
    {
        if (Enabled)
            return;

        MainClient.OnMessage += OnMessage;
        AnonClient.OnMessage += OnMessage;
        await Settings.EnableModule(nameof(LinkCollector));
        return;
    }

    public string Method(string address)
    {
        return string.Join(", ", address.Split(", ").Reverse());
    }

    public async ValueTask Disable()
    {
        if (!Enabled)
            return;

        MainClient.OnMessage -= OnMessage;
        AnonClient.OnMessage -= OnMessage;
        await Settings.DisableModule(nameof(LinkCollector));
        return;
    }

    private record struct LinkData(string Username, string Channel, string LinkText, DateTime TimePosted)
    {
        public static implicit operator LinkData((string, string, string, DateTime) tuple) =>
            new(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
    };
}