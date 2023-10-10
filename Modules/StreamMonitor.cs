using Bot.Models;
using Bot.Services;
using MiniTwitch.Helix.Models;
using MiniTwitch.PubSub.Interfaces;
using MiniTwitch.PubSub.Models;

namespace Bot.Modules;

internal class StreamMonitor: BotModule
{
    private static readonly Dictionary<long, bool> _streams = new();
    private static readonly Dictionary<long, DateTime> _offlineAt = new();
    private static readonly ILogger _logger = ForContext<StreamMonitor>();

    public static bool IsLive(long channelId)
    {
        if (_streams.TryGetValue(channelId, out bool live) && live)
            return true;

        return false;
    }

    public StreamMonitor()
    {
        foreach (long channelId in GetMonitoredChannelIds())
        {
            _streams.Add(channelId, false);
            _offlineAt.Add(channelId, DateTime.MinValue);
        }
    }

    private static async ValueTask OnStreamUp(ChannelId channelId, IStreamUp _)
    {
        if ((DateTime.Now - _offlineAt[channelId]).TotalMinutes <= 5)
            return;

        _streams[channelId] = true;
        ListenResponse r = await TwitchPubSub.ListenTo(Topics.BroadcastSettingsUpdate(channelId));
        if (r.IsSuccess)
            _logger.Debug("Successfully listened to {TopicKey}", r.TopicKey);
        else
            _logger.Warning("Failed to listen to {TopicKey}: {Error}", r.TopicKey, r.Error);

        _logger.Information("{Channel} went live {@StreamData}", ChannelsById[channelId].DisplayName, _);
        string? streamInfo = null;
        if (await HelixApi.Client.GetChannelInformation(channelId) is { Success: true } result)
        {
            Responses.GetChannelInformation.Datum datum = result.Value.Data[0];
            streamInfo = $"{datum.Title} [{datum.GameName}]";
        }

        await MainClient.SendMessage(Config.RelayChannel,
            $"ppBounce @{ChannelsById[channelId].DisplayName} went live! {streamInfo}", true);
    }

    private static async ValueTask OnViewerCountUpdate(ChannelId channelId, IViewerCountUpdate _)
    {
        if ((DateTime.Now - _offlineAt[channelId]).TotalMinutes <= 5)
            return;

        if (!IsLive(channelId))
        {
            ListenResponse r = await TwitchPubSub.ListenTo(Topics.BroadcastSettingsUpdate(channelId));
            if (r.IsSuccess)
                _logger.Debug("Successfully listened to {TopicKey}", r.TopicKey);
            else
                _logger.Warning("Failed to listen to {TopicKey}: {Error}", r.TopicKey, r.Error);

            _logger.Debug("{Channel} is already live: {@StreamData}", ChannelsById[channelId].DisplayName, _);
            _streams[channelId] = true;
        }
    }

    private static async ValueTask OnStreamDown(ChannelId channelId, IStreamDown _)
    {
        _streams[channelId] = false;
        _offlineAt[channelId] = DateTime.Now;
        ListenResponse r = await TwitchPubSub.UnlistenTo(Topics.BroadcastSettingsUpdate(channelId));
        if (r.IsSuccess)
            _logger.Debug("Successfully unlistened to {TopicKey}", r.TopicKey);
        else
            _logger.Warning("Failed to unlisten to {TopicKey}: {Error}", r.TopicKey, r.Error);

        _logger.Information("{Channel} went offline! {@StreamData}", ChannelsById[channelId].DisplayName, _);
        await MainClient.SendMessage(Config.RelayChannel,
            $"Sleepo @{ChannelsById[channelId].DisplayName} is now offline!");
    }

    private ValueTask OnGameChange(ChannelId channelId, IGameChange update)
    {
        return MainClient.SendMessage(Config.RelayChannel,
            $"ApuSkate @{ChannelsById[channelId].DisplayName} changed game: {update.OldGame} ➡ {update.NewGame}");
    }

    private ValueTask OnTitleChange(ChannelId channelId, ITitleChange update)
    {
        return MainClient.SendMessage(Config.RelayChannel,
            $"FeelsDankMan ✏ @{ChannelsById[channelId].DisplayName} changed title: {update.OldTitle} ➡ {update.NewTitle}");
    }

    private static IEnumerable<long> GetMonitoredChannelIds() =>
        Channels.Values.Where(x => x.Priority >= 0).Select(x => x.Id);

    protected override async ValueTask OnModuleEnabled()
    {
        foreach (long channelId in GetMonitoredChannelIds())
            _ = await TwitchPubSub.ListenTo(Topics.VideoPlayback(channelId));

        TwitchPubSub.OnStreamUp += OnStreamUp;
        TwitchPubSub.OnViewerCountUpdate += OnViewerCountUpdate;
        TwitchPubSub.OnStreamDown += OnStreamDown;
        TwitchPubSub.OnTitleChange += OnTitleChange;
        TwitchPubSub.OnGameChange += OnGameChange;
    }

    protected override async ValueTask OnModuleDisabled()
    {
        foreach (long channelId in GetMonitoredChannelIds())
        {
            _ = await TwitchPubSub.UnlistenTo(Topics.VideoPlayback(channelId));
            _ = await TwitchPubSub.UnlistenTo(Topics.BroadcastSettingsUpdate(channelId));
        }

        TwitchPubSub.OnStreamUp -= OnStreamUp;
        TwitchPubSub.OnViewerCountUpdate -= OnViewerCountUpdate;
        TwitchPubSub.OnStreamDown -= OnStreamDown;
        TwitchPubSub.OnTitleChange -= OnTitleChange;
        TwitchPubSub.OnGameChange -= OnGameChange;
    }
}