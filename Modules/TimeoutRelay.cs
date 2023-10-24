using Bot.Models;
using Bot.Utils;
using MiniTwitch.PubSub.Interfaces;
using MiniTwitch.PubSub.Models;

namespace Bot.Modules;

internal class TimeoutRelay: BotModule
{
    private static readonly ILogger _logger = ForContext<TimeoutRelay>();
    private static readonly HttpClient _requests = new();

    private async ValueTask OnTimedOut(UserId userId, ITimeOutData data)
    {
        if (data.ExpiresInMs <= 1000)
        {
            _logger.Debug("You were timed out for {TimeoutDuration}s in #{Channel}: {Reason}", 1, ChannelNameOrId(data.ChannelId), data.Reason);
            return;
        }

        _logger.Information("You were timed out for {TimeoutDuration} in #{Channel}: {Reason}",
            TimeSpan.FromMilliseconds(data.ExpiresInMs), ChannelNameOrId(data.ChannelId), data.Reason);

        string durationString = TimeSpan.FromMilliseconds(data.ExpiresInMs + 50) switch
        {
            { TotalDays: >= 1 } ts => $"{ts.Days}d",
            { TotalHours: >= 1 } ts => $"{ts.Hours}h",
            { TotalMinutes: >= 1 } ts => $"{ts.Minutes}m",
            { TotalSeconds: >= 1 } ts => $"{ts.Seconds}s",
            _ => $"{data.ExpiresInMs}ms",
        };

        DiscordMessageBuilder message = new DiscordMessageBuilder().AddEmbed(e =>
        {
            e.title = $"You were timed out for {durationString} in #{ChannelNameOrId(data.ChannelId)}";
            e.description = data.Reason ?? "NO REASON";
            e.color = 12767488;
            e.timestamp = DateTime.Now;
        });

        await SendDiscordMessage(message);
    }
    private async ValueTask OnBanned(UserId userId, IBanData data)
    {
        _logger.Information("You were banned in #{Channel}: {Reason}", ChannelNameOrId(data.ChannelId), data.Reason);
        DiscordMessageBuilder message = new DiscordMessageBuilder().AddEmbed(e =>
        {
            e.title = $"You were banned in #{ChannelNameOrId(data.ChannelId)}";
            e.description = data.Reason ?? "NO REASON";
            e.color = 16001024;
            e.timestamp = DateTime.Now;
        });

        await SendDiscordMessage(message);
    }
    private async ValueTask OnUntimedOut(UserId userId, IUntimeOutData data)
    {
        _logger.Information("You were untimed out in #{Channel}", ChannelNameOrId(data.ChannelId));
        DiscordMessageBuilder message = new DiscordMessageBuilder().AddEmbed(e =>
        {
            e.title = $"You were untimed out in #{ChannelNameOrId(data.ChannelId)}";
            e.color = 6353920;
            e.timestamp = DateTime.Now;
        });

        await SendDiscordMessage(message);
    }
    private async ValueTask OnUnbanned(UserId userId, IUntimeOutData data)
    {
        _logger.Information("You were unbanned in #{Channel}", ChannelNameOrId(data.ChannelId));
        DiscordMessageBuilder message = new DiscordMessageBuilder().AddEmbed(e =>
        {
            e.title = $"You were unbanned in #{ChannelNameOrId(data.ChannelId)}";
            e.color = 6353920;
            e.timestamp = DateTime.Now;
        });

        await SendDiscordMessage(message);
    }

    private static async Task SendDiscordMessage(DiscordMessageBuilder message)
    {
        try
        {
            HttpResponseMessage response = await _requests.PostAsync(Config.Links["MentionsWebhook"], message.ToStringContent());
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

    private static object ChannelNameOrId(long channelId)
    {
        if (ChannelsById.TryGetValue(channelId, out TwitchChannelDto? channel))
            return channel.ChannelName;

        return channelId;
    }

    protected override async ValueTask OnModuleEnabled()
    {
        ListenResponse response = await TwitchPubSub.ListenTo(Topics.ChatroomsUser(Config.Ids["ParentId"], Config.Secrets["ParentToken"]));
        if (!response.IsSuccess)
            _logger.ForContext("ShowProperties", true).Warning("Failed to listen to {TopicKey}: {Reason}", response.TopicKey, response.Error);

        TwitchPubSub.OnTimedOut += OnTimedOut;
        TwitchPubSub.OnBanned += OnBanned;
        TwitchPubSub.OnUnbanned += OnUnbanned;
        TwitchPubSub.OnUntimedOut += OnUntimedOut;
    }
    protected override async ValueTask OnModuleDisabled()
    {
        ListenResponse response = await TwitchPubSub.UnlistenTo(Topics.ChatroomsUser(Config.Ids["ParentId"], Config.Secrets["ParentToken"]));
        if (!response.IsSuccess)
            _logger.ForContext("ShowProperties", true).Warning("Failed to unlisten to {TopicKey}: {Reason}", response.TopicKey, response.Error);

        TwitchPubSub.OnTimedOut -= OnTimedOut;
        TwitchPubSub.OnBanned -= OnBanned;
        TwitchPubSub.OnUnbanned -= OnUnbanned;
        TwitchPubSub.OnUntimedOut -= OnUntimedOut;
    }
}
