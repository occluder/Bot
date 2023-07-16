using Bot.Interfaces;
using MiniTwitch.PubSub.Interfaces;
using MiniTwitch.PubSub.Models;
using MiniTwitch.PubSub.Models.Payloads;

namespace Bot.Modules;

internal class StreamMonitor : IModule
{
    private static readonly Dictionary<long, bool> _streams = new();
    private static readonly ILogger _logger = ForContext<StreamMonitor>();

    public bool Enabled { get; private set; }

    public static bool IsLive(long channelId)
    {
        if (_streams.TryGetValue(channelId, out bool live) && live)
            return true;

        return false;
    }

    public StreamMonitor()
    {
        foreach (long channelId in GetMonitoredChannelIds())
            _streams.Add(channelId, false);
    }

    private async ValueTask OnStreamUp(ChannelId channelId, IStreamUp _)
    {
        _streams[channelId] = true;
        ListenResponse r = await TwitchPubSub.ListenTo(Topics.BroadcastSettingsUpdate(channelId));
        if (r.IsSuccess)
            _logger.Debug("Successfully listened to {TopicKey}", r.TopicKey);
        else
            _logger.Warning("Failed to listen to {TopicKey}: {Error}", r.TopicKey, r.Error);

        _logger.Information("{@StreamData}", _);
        await MainClient.SendMessage(Config.RelayChannel, $"ppBounce @{ChannelsById[channelId].DisplayName} went live!");
    }

    private async ValueTask OnViewerCountUpdate(ChannelId channelId, IViewerCountUpdate _)
    {
        if (!IsLive(channelId))
        {
            ListenResponse r = await TwitchPubSub.ListenTo(Topics.BroadcastSettingsUpdate(channelId));
            if (r.IsSuccess)
                _logger.Debug("Successfully listened to {TopicKey}", r.TopicKey);
            else
                _logger.Warning("Failed to listen to {TopicKey}: {Error}", r.TopicKey, r.Error);

            _logger.Debug("{ChannelId} is already live: {@StreamData}", channelId.Value, _);
            await MainClient.SendMessage(Config.RelayChannel, $"ppCircle @{ChannelsById[channelId].DisplayName} is live!");
            _streams[channelId] = true;
        }
    }

    private async ValueTask OnStreamDown(ChannelId channelId, IStreamDown _)
    {
        _streams[channelId] = false;
        ListenResponse r = await TwitchPubSub.UnlistenTo(Topics.BroadcastSettingsUpdate(channelId));
        if (r.IsSuccess)
            _logger.Debug("Successfully unlistened to {TopicKey}", r.TopicKey);
        else
            _logger.Warning("Failed to unlisten to {TopicKey}: {Error}", r.TopicKey, r.Error);

        _logger.Information("{@StreamData}", _);
        await MainClient.SendMessage(Config.RelayChannel, $"Sleepo @{ChannelsById[channelId].DisplayName} is now offline!");
    }

    private ValueTask OnBroadcastSettingsUpdate(ChannelId channelId, BroadcastSettingsUpdate settings)
    {
        if (settings.OldTitle == settings.NewTitle)
            return MainClient.SendMessage(Config.RelayChannel, $"ppSlide @{ChannelsById[channelId].DisplayName} changed game: {settings.OldGame} -> {settings.OldGame}");
        else if (settings.OldGame == settings.NewGame)
            return MainClient.SendMessage(Config.RelayChannel, $"ppSlide @{ChannelsById[channelId].DisplayName} changed title: {settings.OldTitle} -> {settings.NewTitle}");

        return MainClient.SendMessage(Config.RelayChannel, $"ppSlide @{ChannelsById[channelId].DisplayName} updated their: {settings.OldTitle} -> {settings.NewTitle} -- {settings.OldGame} -> {settings.NewGame}");
    }


    private static IEnumerable<long> GetMonitoredChannelIds() => Channels.Values.Where(x => x.Priority >= 0).Select(x => x.Id);

    public async ValueTask Enable()
    {
        if (this.Enabled)
            return;

        foreach (long channelId in GetMonitoredChannelIds())
            _ = await TwitchPubSub.ListenTo(Topics.VideoPlayback(channelId));

        TwitchPubSub.OnStreamUp += OnStreamUp;
        TwitchPubSub.OnViewerCountUpdate += OnViewerCountUpdate;
        TwitchPubSub.OnStreamDown += OnStreamDown;
        TwitchPubSub.OnBroadcastSettingsUpdate += OnBroadcastSettingsUpdate;
        this.Enabled = true;
        await Settings.EnableModule(nameof(StreamMonitor));
    }

    public async ValueTask Disable()
    {
        if (!this.Enabled)
            return;

        foreach(long channelId in GetMonitoredChannelIds())
        {
            _ = await TwitchPubSub.UnlistenTo(Topics.VideoPlayback(channelId));
            _ = await TwitchPubSub.UnlistenTo(Topics.BroadcastSettingsUpdate(channelId));
        }

        TwitchPubSub.OnStreamUp -= OnStreamUp;
        TwitchPubSub.OnViewerCountUpdate -= OnViewerCountUpdate;
        TwitchPubSub.OnStreamDown -= OnStreamDown;
        TwitchPubSub.OnBroadcastSettingsUpdate -= OnBroadcastSettingsUpdate;
        this.Enabled = false;
        await Settings.DisableModule(nameof(StreamMonitor));
    }
}
