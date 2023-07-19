using System.Text.RegularExpressions;
using Bot.Interfaces;
using Bot.Utils;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class MentionsRelay : IModule
{
    public bool Enabled { get; private set; }

    private readonly Regex _regex = new(Config.MentionsRegex, RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private readonly HttpClient _requests = new() { Timeout = TimeSpan.FromSeconds(15) };

    private async ValueTask OnMessage(Privmsg message)
    {
        if (BlackListedUserIds.Contains(message.Author.Id))
            return;

        if (_regex.Match(message.Content) is { Success: true } match)
        {
            var builder = new DiscordMessageBuilder().AddEmbed(embed =>
            {
                embed.title = $"@{message.Author.Name} in #{message.Channel.Name}";
                embed.description = message.Content;
                embed.timestamp = DateTime.Now;
            });

            HttpResponseMessage response;
            try
            {
                response = await _requests.PostAsync(Config.MentionsWebhookUrl, builder.ToStringContent());
                if (response.IsSuccessStatusCode)
                    ForContext<MentionsRelay>().Debug("[{StatusCode}] POST {Url}", response.StatusCode, Config.MentionsWebhookUrl);
                else
                    ForContext<MentionsRelay>().Warning("[{StatusCode}] POST {Url}", response.StatusCode, Config.MentionsWebhookUrl);
            }
            catch (Exception ex)
            {
                ForContext<MentionsRelay>().Error(ex, "POST {Url}", Config.MentionsWebhookUrl);
            }
        }
    }

    public async ValueTask Enable()
    {
        if (this.Enabled)
            return;

        MainClient.OnMessage += OnMessage;
        AnonClient.OnMessage += OnMessage;
        this.Enabled = true;
        await Settings.EnableModule(nameof(MentionsRelay));
    }

    public async ValueTask Disable()
    {
        if (!this.Enabled)
            return;

        MainClient.OnMessage -= OnMessage;
        AnonClient.OnMessage -= OnMessage;
        this.Enabled = false;
        await Settings.DisableModule(nameof(MentionsRelay));
    }
}
