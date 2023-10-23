using Bot.Models;
using MiniTwitch.Helix.Enums;
using MiniTwitch.Helix.Models;
using MiniTwitch.PubSub.Interfaces;
using MiniTwitch.PubSub.Models;

namespace Bot.Modules;

internal class StreamMonitor: BotModule
{
    private static readonly Dictionary<long, bool> _streams = new();
    private static readonly Dictionary<long, DateTime> _offlineAt = new();
    private static readonly ILogger _logger = ForContext<StreamMonitor>();

    public StreamMonitor()
    {
        foreach (long channelId in GetMonitoredChannelIds())
        {
            _streams.Add(channelId, false);
            _offlineAt.Add(channelId, DateTime.MinValue);
        }
    }

    public static bool IsLive(long channelId) => _streams.TryGetValue(channelId, out bool live) && live;

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

        _logger.Information("{Channel} went live", ChannelsById[channelId].DisplayName);
        string? streamInfo = null;
        if (await HelixClient.GetChannelInformation(channelId) is { Success: true } result)
        {
            Responses.GetChannelInformation.Datum datum = result.Value.Data[0];
            streamInfo = $"{datum.Title} [{datum.GameName}]";
        }

        HelixResult cResult = await HelixClient.UpdateUserChatColor(Config.Ids["BotId"], ChatColor.green);
        await MainClient.SendMessage(Config.RelayChannel,
            $"ppBounce @{ChannelsById[channelId].DisplayName} went live! {streamInfo}",
            cResult.Success);
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

            _logger.Debug("{Channel} is already live", ChannelsById[channelId].DisplayName);
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

        _logger.Information("{Channel} went offline!", ChannelsById[channelId].DisplayName);
        HelixResult result = await HelixClient.UpdateUserChatColor(Config.Ids["BotId"], ChatColor.orange_red);
        await MainClient.SendMessage(Config.RelayChannel,
            $"Sleepo @{ChannelsById[channelId].DisplayName} is now offline!",
            result.Success);
    }

    private static async ValueTask OnGameChange(ChannelId channelId, IGameChange update)
    {
        HelixResult result = await HelixClient.UpdateUserChatColor(Config.Ids["BotId"], ChatColor.dodger_blue);
        await MainClient.SendMessage(Config.RelayChannel,
            $"ApuSkate @{ChannelsById[channelId].DisplayName} changed game: {update.OldGame} ➡ {update.NewGame}",
            result.Success);
    }

    private static async ValueTask OnTitleChange(ChannelId channelId, ITitleChange update)
    {
        HelixResult result = await HelixClient.UpdateUserChatColor(Config.Ids["BotId"], ChatColor.dodger_blue);
        await MainClient.SendMessage(Config.RelayChannel,
            $"ApuSkate @{ChannelsById[channelId].DisplayName} changed title: {update.OldTitle} ➡ {update.NewTitle}",
            result.Success);
    }

    private static IEnumerable<long> GetMonitoredChannelIds() =>
        Channels.Values.Where(x => x.Priority >= 0).Select(x => x.Id);

    protected override async ValueTask OnModuleEnabled()
    {
        _ = await TwitchPubSub.ListenTo(GetMonitoredChannelIds().Select(x => Topics.VideoPlayback(x)));
        TwitchPubSub.OnStreamUp += OnStreamUp;
        TwitchPubSub.OnViewerCountUpdate += OnViewerCountUpdate;
        TwitchPubSub.OnStreamDown += OnStreamDown;
        TwitchPubSub.OnTitleChange += OnTitleChange;
        TwitchPubSub.OnGameChange += OnGameChange;
    }

    protected override async ValueTask OnModuleDisabled()
    {
        _ = await TwitchPubSub.UnlistenTo(TwitchPubSub.ActiveTopics.Where(x =>
            x.Key.StartsWith("video-playback-by-id.") || x.Key.StartsWith("broadcast-settings-update."))
        );

        TwitchPubSub.OnStreamUp -= OnStreamUp;
        TwitchPubSub.OnViewerCountUpdate -= OnViewerCountUpdate;
        TwitchPubSub.OnStreamDown -= OnStreamDown;
        TwitchPubSub.OnTitleChange -= OnTitleChange;
        TwitchPubSub.OnGameChange -= OnGameChange;
    }
}