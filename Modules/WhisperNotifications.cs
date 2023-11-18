using System.Net.Http.Json;
using Bot.Models;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;
internal class WhisperNotifications: BotModule
{
    private static readonly HttpClient _requests = new();
    private readonly ILogger _logger = ForContext<WhisperNotifications>();
    private readonly IrcClient _whisperClient;

    public WhisperNotifications()
    {
        _whisperClient = new(options =>
        {
            options.Username = "whatever";
            options.OAuth = Config.Secrets["ParentToken"];
            options.ReconnectionDelay = TimeSpan.FromMinutes(5);
        });

        _whisperClient.DefaultLogger.Enabled = false;
    }

    private async ValueTask OnWhisperReceived(Whisper whisper)
    {
        if (UserBlacklisted(whisper.Author.Id))
            return;

        var payload = new
        {
            content = Config.Secrets["ParentHandle"],
            embeds = new[]
            {
                new
                {
                    title = $"@`{whisper.Author.Name}` ({whisper.Author.Id}) sent you a whisper",
                    color = 2393480,
                    description = whisper.Content,
                    timestamp = DateTime.Now
                }
            }
        };

        _logger.Information("{@WhisperAuthor} sent you a whisper: {WhisperContent}", whisper.Author, whisper.Content);
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

    protected override async ValueTask OnModuleEnabled()
    {
        _whisperClient.OnWhisper += OnWhisperReceived;
        _ = await _whisperClient.ConnectAsync();
        _ = await _whisperClient.JoinChannel(Config.RelayChannel);
    }

    protected override async ValueTask OnModuleDisabled()
    {
        _whisperClient.OnWhisper -= OnWhisperReceived;
        await _whisperClient.DisconnectAsync();
    }
}
