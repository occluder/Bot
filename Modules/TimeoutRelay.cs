using Bot.Interfaces;
using MiniTwitch.PubSub.Interfaces;
using MiniTwitch.PubSub.Models;

namespace Bot.Modules;

internal class TimeoutRelay : IModule
{
    private static readonly ILogger _logger = ForContext<TimeoutRelay>();

    public bool Enabled { get; private set; }

    private ValueTask OnTimedOut(UserId userId, ITimeOutData data)
    {
        if (data.ExpiresInMs <= 1000)
            _logger.Debug("You were timed out for {TimeoutDuration}s in #{Channel}: {Reason}", 1, ChannelNameOrId(data.ChannelId), data.Reason);
        else
            _logger.Information("You were timed out for {TimeoutDuration} in #{Channel}: {Reason}", TimeSpan.FromMilliseconds(data.ExpiresInMs), ChannelNameOrId(data.ChannelId), data.Reason);

        return default;
    }
    private ValueTask OnBanned(UserId userId, IBanData data)
    {
        _logger.Information("You were banned in #{Channel}: {Reason}", ChannelNameOrId(data.ChannelId), data.Reason);
        return default;
    }
    private ValueTask OnUntimedOut(UserId userId, IUntimeOutData data)
    {
        _logger.Information("You were untimed out in #{Channel}", ChannelNameOrId(data.ChannelId));
        return default;
    }
    private ValueTask OnUnbanned(UserId userId, IUntimeOutData data)
    {
        _logger.Information("You were unbanned in #{Channel}", ChannelNameOrId(data.ChannelId));
        return default;
    }

    private static object ChannelNameOrId(long channelId)
    {
        if (ChannelsById.TryGetValue(channelId, out var channel))
            return channel.Username;

        return channelId;
    }

    public async ValueTask Enable()
    {
        if (this.Enabled)
            return;

        ListenResponse response = await TwitchPubSub.ListenTo(Topics.ChatroomsUser(Config.ParentId, overrideToken: Config.ParentToken));
        if (!response.IsSuccess)
            _logger.ForContext("ShowProperties", true).Warning("Failed to listen to {TopicKey}: {Reason}", response.TopicKey, response.Error);

        TwitchPubSub.OnTimedOut += OnTimedOut;
        TwitchPubSub.OnBanned += OnBanned;
        TwitchPubSub.OnUnbanned += OnUnbanned;
        TwitchPubSub.OnUntimedOut += OnUntimedOut;
        this.Enabled = true;
        await Settings.EnableModule(nameof(TimeoutRelay));
    }

    public async ValueTask Disable()
    {
        if (!this.Enabled)
            return;

        ListenResponse response = await TwitchPubSub.UnlistenTo(Topics.ChatroomsUser(Config.ParentId, overrideToken: Config.ParentToken));
        if (!response.IsSuccess)
            _logger.ForContext("ShowProperties", true).Warning("Failed to unlisten to {TopicKey}: {Reason}", response.TopicKey, response.Error);

        TwitchPubSub.OnTimedOut -= OnTimedOut;
        TwitchPubSub.OnBanned -= OnBanned;
        TwitchPubSub.OnUnbanned -= OnUnbanned;
        TwitchPubSub.OnUntimedOut -= OnUntimedOut;
        this.Enabled = false;
        await Settings.DisableModule(nameof(TimeoutRelay));
    }
}
