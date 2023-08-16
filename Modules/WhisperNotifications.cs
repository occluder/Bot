using Bot.Interfaces;
using Bot.Utils;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;
internal class WhisperNotifications : IModule
{
    private static readonly HttpClient _requests = new();

    public bool Enabled { get; private set; }
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

        _whisperClient.OnWhisper += OnWhisperReceived;
    }

    private async ValueTask OnWhisperReceived(Whisper whisper)
    {
        if (BlackListedUserIds.Contains(whisper.Author.Id))
            return;

        var builder = new DiscordMessageBuilder(Config.Secrets["ParentHandle"]).AddEmbed(embed =>
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

    public async ValueTask Enable()
    {
        if (this.Enabled)
            return;

        await _whisperClient.ConnectAsync();
        await _whisperClient.JoinChannel(Config.RelayChannel);
        this.Enabled = true;
        await Settings.EnableModule(nameof(WhisperNotifications));
    }

    public async ValueTask Disable()
    {
        if (!this.Enabled)
            return;

        await _whisperClient.DisconnectAsync();
        this.Enabled = false;
        await Settings.DisableModule(nameof(WhisperNotifications));
    }
}
