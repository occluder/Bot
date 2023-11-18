using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class MentionsRelay: BotModule
{
    private static readonly ILogger _logger = ForContext<MentionsRelay>();

    private readonly Regex _imageHosts = new(Config.Secrets["ImageHostsRegex"], RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    private readonly Regex _regex = new(Config.Secrets["MentionsRegex"], RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(50));

    private readonly HttpClient _requests = new() { Timeout = TimeSpan.FromSeconds(15) };

    private async ValueTask OnMessage(Privmsg message)
    {
        if (IsBlacklisted(message.Author.Id))
            return;

        if (_regex.Match(message.Content) is { Success: false })
            return;

        object? payload = null;
        if (message.Reply.HasContent)
            payload = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = $"@`{message.Reply.ParentUsername}` said in #`{message.Channel.Name}`",
                        description = message.Reply.ParentMessage,
                        timestamp = DateTime.Now,
                        fields = new[]
                        {
                            new
                            {
                                name = $"@`{message.Author.Name}` replied:",
                                value = message.Content
                            }
                        },
                        image = _imageHosts.Match(message.Content) is { Success: true } imageMatch
                            ? new
                            {
                                url = imageMatch.Value
                            }
                            : null
                    }
                }
            };
        else
            payload = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = $"@`{message.Author.Name}` in #`{message.Channel.Name}`",
                        description = message.Content,
                        timestamp = DateTime.Now
                    }
                }
            };

        try
        {
            HttpResponseMessage response = await _requests.PostAsJsonAsync(Config.Links["MentionsWebhook"], payload);
            if (response.IsSuccessStatusCode)
                _logger.Debug("[{StatusCode}] POST {Url}", response.StatusCode, Config.Links["MentionsWebhook"]);
            else
                _logger.Warning("[{StatusCode}] POST {Url}", response.StatusCode, Config.Links["MentionsWebhook"]);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "POST {Url}", Config.Links["MentionsWebhook"]);
        }
    }

    protected override ValueTask OnModuleEnabled()
    {
        MainClient.OnMessage += OnMessage;
        AnonClient.OnMessage += OnMessage;
        return default;
    }

    protected override ValueTask OnModuleDisabled()
    {
        MainClient.OnMessage -= OnMessage;
        AnonClient.OnMessage -= OnMessage;
        return default;
    }
}