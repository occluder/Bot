using Bot.Models;
using Bot.Utils;
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
    }

    private async ValueTask OnWhisperReceived(Whisper whisper)
    {
        if (BlackListedUserIds.Contains(whisper.Author.Id))
            return;

        DiscordMessageBuilder builder = new DiscordMessageBuilder(Config.Secrets["ParentHandle"]).AddEmbed(embed =>
        {
            embed.title = $"@`{whisper.Author.Name}` sent you a whisper";
            embed.color = 2393480;
            embed.description = whisper.Content;
            embed.timestamp = DateTime.Now;
        });

        _logger.Information("{@WhisperAuthor} sent you a whisper: {WhisperContent}", whisper.Author, whisper.Content);
        HttpResponseMessage response;
        try
        {
            response = await _requests.PostAsync(Config.Links["MentionsWebhook"], builder.ToStringContent());
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
