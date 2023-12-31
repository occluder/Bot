using System.Net.Http.Json;
using Bot.Models;
using MiniTwitch.PubSub.Interfaces;
using MiniTwitch.PubSub.Models;

namespace Bot.Modules;

internal class TimeoutRelay: BotModule
{
    private static readonly ILogger _logger = ForContext<TimeoutRelay>();
    private static readonly HttpClient _requests = new();

    private static async ValueTask OnTimedOut(UserId userId, ITimeOutData data)
    {
        if (data.ExpiresInMs <= 1000)
        {
            _logger.Debug(
                "You were timed out for {TimeoutDuration}s in #{Channel}: {Reason}",
                1,
                await GetChannelName(data.ChannelId),
                data.Reason
            );

            return;
        }

        _logger.Information(
            "You were timed out for {TimeoutDuration} in #{Channel}: {Reason}",
            TimeSpan.FromMilliseconds(data.ExpiresInMs),
            await GetChannelName(data.ChannelId),
            data.Reason
        );

        string durationString = TimeSpan.FromMilliseconds(data.ExpiresInMs + 50) switch
        {
            { TotalDays: >= 1 } ts => $"{ts.Days}d",
            { TotalHours: >= 1 } ts => $"{ts.Hours}h",
            { TotalMinutes: >= 1 } ts => $"{ts.Minutes}m",
            { TotalSeconds: >= 1 } ts => $"{ts.Seconds}s",
            _ => $"{data.ExpiresInMs}ms"
        };

        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = $"You were timed out for {durationString} in #{await GetChannelName(data.ChannelId)}",
                    description = data.Reason ?? "NO REASON",
                    color = 12767488,
                    timestamp = DateTime.Now
                }
            }
        };

        await SendDiscordMessage(payload);
    }

    private static async ValueTask OnBanned(UserId userId, IBanData data)
    {
        _logger.Information(
            "You were banned in #{Channel}: {Reason}",
            await GetChannelName(data.ChannelId),
            data.Reason
        );

        var payload = new
        {
            content = Config.Secrets["ParentHandle"],
            embeds = new[]
            {
                new
                {
                    title = $"You were banned in #{await GetChannelName(data.ChannelId)}",
                    description = data.Reason ?? "NO REASON",
                    color = 16001024,
                    timestamp = DateTime.Now
                }
            }
        };

        await SendDiscordMessage(payload);
    }

    private static async ValueTask OnUntimedOut(UserId userId, IUntimeOutData data)
    {
        _logger.Information("You were untimed out in #{Channel}", await GetChannelName(data.ChannelId));
        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = $"You were untimed out in #{await GetChannelName(data.ChannelId)}",
                    color = 6353920,
                    timestamp = DateTime.Now
                }
            }
        };

        await SendDiscordMessage(payload);
    }

    private static async ValueTask OnUnbanned(UserId userId, IUntimeOutData data)
    {
        _logger.Information("You were unbanned in #{Channel}", await GetChannelName(data.ChannelId));
        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = $"You were unbanned in #{await GetChannelName(data.ChannelId)}",
                    color = 6353920,
                    timestamp = DateTime.Now
                }
            }
        };

        await SendDiscordMessage(payload);
    }

    private static async Task SendDiscordMessage(object message)
    {
        try
        {
            HttpResponseMessage response = await _requests.PostAsJsonAsync(Config.Links["MentionsWebhook"], message);
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

    private static async Task<string> GetChannelName(long channelId)
    {
        if (await HelixClient.GetUsers(channelId) is { Success: true } result) return result.Value.Data[0].Name;

        return channelId.ToString();
    }

    protected override async ValueTask OnModuleEnabled()
    {
        ListenResponse response =
            await TwitchPubSub.ListenTo(Topics.ChatroomsUser(Config.Ids["ParentId"], Config.Secrets["ParentToken"]));
        if (!response.IsSuccess)
            _logger.ForContext("ShowProperties", true).Warning("Failed to listen to {TopicKey}: {Reason}",
                response.TopicKey, response.Error);

        TwitchPubSub.OnTimedOut += OnTimedOut;
        TwitchPubSub.OnBanned += OnBanned;
        TwitchPubSub.OnUnbanned += OnUnbanned;
        TwitchPubSub.OnUntimedOut += OnUntimedOut;
    }

    protected override async ValueTask OnModuleDisabled()
    {
        ListenResponse response =
            await TwitchPubSub.UnlistenTo(Topics.ChatroomsUser(Config.Ids["ParentId"], Config.Secrets["ParentToken"]));
        if (!response.IsSuccess)
            _logger.ForContext("ShowProperties", true).Warning("Failed to unlisten to {TopicKey}: {Reason}",
                response.TopicKey, response.Error);

        TwitchPubSub.OnTimedOut -= OnTimedOut;
        TwitchPubSub.OnBanned -= OnBanned;
        TwitchPubSub.OnUnbanned -= OnUnbanned;
        TwitchPubSub.OnUntimedOut -= OnUntimedOut;
    }
}