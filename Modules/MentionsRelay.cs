using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bot.Models;
using Discord.Webhook;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal class MentionsRelay: BotModule
{
    const int MAX_HISTORY_MESSAGES = 20;
    static readonly ILogger _logger = ForContext<MentionsRelay>();
    static readonly Regex _imageHosts = new(Config.Secrets["ImageHostsRegex"], RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    static readonly Regex _regex = new(Config.Secrets["MentionsRegex"], RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    readonly Webhook _webhook;

    public MentionsRelay()
    {
        _webhook = new(Config.Links["MentionsWebhook"]);
    }

    private async ValueTask OnMessage(Privmsg message)
    {
        if (UserBlacklisted(message.Author.Id) || Channels[message.Channel.Name].NoRelay)
        {
            return;
        }

        var match = _regex.Match(message.Content);
        if (!match.Success)
        {
            return;
        }

        string pfp = (await HelixClient.GetUsers(message.Author.Id)).Value!.Data[0].ProfileImageUrl;
        WebhookObject payload = new();
        if (message.Reply.HasContent)
        {
            payload.AddEmbed(embed =>
                embed.WithTitle($"@`{message.Reply.ParentUsername}`")
                     .WithDescription(message.Reply.ParentMessage)
                     .WithColor(ColorToDColor(message.Author.ChatColor))
                     .WithThumbnail(pfp)
                     .WithFooter(message.Channel.Name, Channels[message.Channel.Name].AvatarUrl)
                     .AddField($"@`{message.Author.Name}` replied:", message.Content)
            );
        }
        else
        {
            payload.AddEmbed(embed =>
                embed.WithTitle($"@`{message.Author.Name}`")
                     .WithDescription(message.Content)
                     .WithColor(ColorToDColor(message.Author.ChatColor))
                     .WithThumbnail(pfp)
                     .WithFooter(message.Channel.Name, Channels[message.Channel.Name].AvatarUrl)
            );
        }

        if (_imageHosts.Match(message.Content) is { Success: true } image)
        {
            payload.embeds[0].image = new() { url = image.Value };
        }

        string[]? content = await GetRecentChannelMessages(message.Channel.Id, message.SentTimestamp.UtcDateTime);
        if (content is not null)
        {
            payload.content = $"```ansi\n{string.Join("\n", content.Reverse())}\n```";
        }

        try
        {
            await _webhook.SendAsync(payload);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error sending webhook");
        }
    }

    static async Task<string[]?> GetRecentChannelMessages(long channelId, DateTime time)
    {
        var response = await GetFromRequest<ChannelHistroy>(
            $"https://logs.ivr.fi/channelid/{channelId}?to={time.AddSeconds(5):O}&json=true&reverse=true&limit={MAX_HISTORY_MESSAGES}",
            _jsonOptions
        );

        return response.Match<string[]?>(
            success =>
            {
                return AnsiMessage(success.Messages.Where(x => x.Type == 1));
            },
            badStatus =>
            {
                _logger.Error("Error fetching messages: {StatusCode}", badStatus);
                return null;
            },
            exception =>
            {
                _logger.Error(exception, "Error fetching messages");
                return null;
            }
        );
    }

    static string[] AnsiMessage(IEnumerable<Message> messages)
    {
        var final = new string[MAX_HISTORY_MESSAGES];
        int totalLength = 0;
        int i = 0;
        foreach (var message in messages)
        {
            StringBuilder sb = new();
            sb.Append($"\u001b[2;30m[{message.Timestamp:HH:mm:ss}]\u001b[0m ");
            sb.Append($"\u001b[2;33m{message.Username}\u001b[0m: ");
            if (_regex.Match(message.Text) is { Success: true } match)
            {
                sb.Append(_regex.Replace(message.Text, $"\u001b[2;35m{match.Value}\u001b[0m"));
            }
            else
            {
                sb.Append(message.Text);
            }

            if (totalLength + sb.Length > 980)
            {
                break;
            }

            final[i] = sb.ToString();
            totalLength += sb.Length;
            i++;
        }

        return final;
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

record ChannelHistroy(Message[] Messages);
record Message(
    string Text,
    DateTime Timestamp,
    string Channel,
    int Type,
    string Username
);