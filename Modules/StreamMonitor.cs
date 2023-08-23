﻿using Bot.Models;
using MiniTwitch.PubSub.Interfaces;
using MiniTwitch.PubSub.Models;
using MiniTwitch.PubSub.Models.Payloads;

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

    private async ValueTask OnStreamUp(ChannelId channelId, IStreamUp _)
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
        await MainClient.SendMessage(Config.RelayChannel, $"🟩 ppBounce @{ChannelsById[channelId].DisplayName} went live!", true);
    }

    private async ValueTask OnViewerCountUpdate(ChannelId channelId, IViewerCountUpdate _)
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

    private async ValueTask OnStreamDown(ChannelId channelId, IStreamDown _)
    {
        _streams[channelId] = false;
        _offlineAt[channelId] = DateTime.Now;
        ListenResponse r = await TwitchPubSub.UnlistenTo(Topics.BroadcastSettingsUpdate(channelId));
        if (r.IsSuccess)
            _logger.Debug("Successfully unlistened to {TopicKey}", r.TopicKey);
        else
            _logger.Warning("Failed to unlisten to {TopicKey}: {Error}", r.TopicKey, r.Error);

        _logger.Information("{Channel} went offline! {@StreamData}", ChannelsById[channelId].DisplayName, _);
        await MainClient.SendMessage(Config.RelayChannel, $"🟥 Sleepo @{ChannelsById[channelId].DisplayName} is now offline!");
    }

    private ValueTask OnBroadcastSettingsUpdate(ChannelId channelId, BroadcastSettingsUpdate settings)
    {
        if (settings.OldTitle != settings.NewTitle && settings.OldGame != settings.NewGame)
            return MainClient.SendMessage(Config.RelayChannel, $"🟦 ppSlide @{ChannelsById[channelId].DisplayName} updated their stream: {settings.OldTitle} -> {settings.NewTitle} -- {settings.OldGame} -> {settings.NewGame}");
        else if (settings.OldTitle != settings.NewTitle)
            return MainClient.SendMessage(Config.RelayChannel, $"🟦 ppSlide @{ChannelsById[channelId].DisplayName} changed title: {settings.OldTitle} -> {settings.NewTitle}");
        else if (settings.OldGameId != settings.NewGameId)
            return MainClient.SendMessage(Config.RelayChannel, $"🟦 ppSlide @{ChannelsById[channelId].DisplayName} changed game: {settings.OldGame} -> {settings.NewGame}");

        return ValueTask.CompletedTask;
    }


    private static IEnumerable<long> GetMonitoredChannelIds() => Channels.Values.Where(x => x.Priority >= 0).Select(x => x.Id);

    protected override async ValueTask OnModuleEnabled()
    {
        foreach (long channelId in GetMonitoredChannelIds())
            _ = await TwitchPubSub.ListenTo(Topics.VideoPlayback(channelId));

        TwitchPubSub.OnStreamUp += OnStreamUp;
        TwitchPubSub.OnViewerCountUpdate += OnViewerCountUpdate;
        TwitchPubSub.OnStreamDown += OnStreamDown;
        TwitchPubSub.OnBroadcastSettingsUpdate += OnBroadcastSettingsUpdate;
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
        TwitchPubSub.OnBroadcastSettingsUpdate -= OnBroadcastSettingsUpdate;
    }
}
