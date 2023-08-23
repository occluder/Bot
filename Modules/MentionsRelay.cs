using System.Text.RegularExpressions;
using Bot.Models;
using Bot.Utils;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class MentionsRelay: BotModule
{
    private readonly Regex _imageHosts = new(Config.Secrets["ImageHostsRegex"], RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private readonly Regex _regex = new(Config.Secrets["MentionsRegex"], RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private readonly HttpClient _requests = new() { Timeout = TimeSpan.FromSeconds(15) };

    private async ValueTask OnMessage(Privmsg message)
    {
        if (BlackListedUserIds.Contains(message.Author.Id))
            return;

        if (_regex.Match(message.Content) is { Success: true } match)
        {
            DiscordMessageBuilder builder = new DiscordMessageBuilder().AddEmbed(embed =>
            {
                embed.title = $"@{message.Author.Name} in #{message.Channel.Name}";
                embed.description = message.Content;
                embed.timestamp = DateTime.Now;
                if (message.Reply.HasContent)
                {
                    _ = embed.AddField(field =>
                    {
                        field.name = $"Replying to @{message.Reply.ParentUsername}";
                        field.value = message.Reply.ParentMessage;
                    });
                }

                if (_imageHosts.Match(message.Content) is { Success: true } imageMatch)
                    _ = embed.SetImage(i => i.url = imageMatch.Value);
            });

            HttpResponseMessage response;
            try
            {
                response = await _requests.PostAsync(Config.Links["MentionsWebhook"], builder.ToStringContent());
                if (response.IsSuccessStatusCode)
                    ForContext<MentionsRelay>().Debug("[{StatusCode}] POST {Url}", response.StatusCode, Config.Links["MentionsWebhook"]);
                else
                    ForContext<MentionsRelay>().Warning("[{StatusCode}] POST {Url}", response.StatusCode, Config.Links["MentionsWebhook"]);
            }
            catch (Exception ex)
            {
                ForContext<MentionsRelay>().Error(ex, "POST {Url}", Config.Links["MentionsWebhook"]);
            }
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
