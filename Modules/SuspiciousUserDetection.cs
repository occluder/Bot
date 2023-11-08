using Bot.Models;
using Bot.StartupTasks;
using CachingFramework.Redis.Contracts.RedisObjects;
using MiniTwitch.Helix.Enums;
using MiniTwitch.Helix.Models;
using MiniTwitch.Helix.Responses;
using MiniTwitch.Irc.Interfaces;
using MiniTwitch.PubSub.Models;
using MiniTwitch.PubSub.Payloads;

namespace Bot.Modules;

public class SuspiciousUserDetection: BotModule
{
    private const int SUS_MAX_AGE_DAYS = 15;
    
    private static readonly HashSet<SuspiciousUser> _suspiciousUserIds = SuspiciousUsers.ToHashSet();
    private static readonly ILogger _logger = ForContext<SuspiciousUserDetection>();
    private static long _highestNonSuspiciousId;

    private static IRedisSet<SuspiciousUser> SuspiciousUsers =>
        Collections.GetRedisSet<SuspiciousUser>("bot:chat:suspicious_users");

    private static async ValueTask OnFollow(ChannelId channelId, Follower follower)
    {
        if (follower.Id < _highestNonSuspiciousId) return;
        HelixResult<Users> result = await HelixClient.GetUsers(follower.Id);
        if (result is { Success: true })
        {
            DateTime createdAt = result.Value.Data[0].CreatedAt.ToUniversalTime();
            if (createdAt > DateTime.UtcNow.AddDays(-SUS_MAX_AGE_DAYS))
            {
                await AddSuspiciousUser(follower.Id, channelId);
                _ = await HelixClient.UpdateUserChatColor(ChatColor.GoldenRod);
                await MainClient.SendMessage(
                    Config.RelayChannel,
                    $"susLada {follower.Name} #{ChannelNameOrId(channelId)} {GetString(result.Value.Data[0].CreatedAt)}",
                    true
                );

                _logger.Information("New sus user: {UserId}, #{ChannelId}", follower.Id, ChannelNameOrId(channelId));
                return;
            }

            if (_highestNonSuspiciousId < follower.Id) _highestNonSuspiciousId = follower.Id;
            _logger.Information("Updated highest non-suspicious id to {NewId}", follower.Id);
            await Cleanup();
            return;
        }

        _logger.Error("Failed to get user with id {Id}: {@HelixResult}", follower.Id, result);
    }

    private static async ValueTask OnBan(IUserBan arg)
    {
        var user = new SuspiciousUser(arg.Target.Id, arg.Channel.Id);
        if (!_suspiciousUserIds.Contains(user)) return;
        _suspiciousUserIds.Remove(user);
        SuspiciousUsers.Remove(user);
        _logger.Information(
            "Suspicious user {SuspiciousUser} banned from #{Channel}",
            arg.Target.Name,
            arg.Channel.Name
        );

        _ = await HelixClient.UpdateUserChatColor(ChatColor.HotPink);
        await MainClient.SendMessage(
            Config.RelayChannel,
            $"ℹ\ufe0f Suspicious user {arg.Target.Name} has been banned from #{arg.Channel.Name}",
            true
        );
    }

    private static async Task AddSuspiciousUser(long id, long channelId)
    {
        var user = new SuspiciousUser(id, channelId);
        _suspiciousUserIds.Add(user);
        await SuspiciousUsers.AddAsync(user);
        _logger.Debug("Suspicious user with {Id} added", id);
    }

    private static async Task Cleanup()
    {
        foreach (SuspiciousUser suspiciousUser in _suspiciousUserIds.Where(x => x.UserId < _highestNonSuspiciousId))
        {
            _suspiciousUserIds.Remove(suspiciousUser);
            await SuspiciousUsers.RemoveAsync(suspiciousUser);
            _logger.Information("User marked as not suspicious anymore: {@User}", suspiciousUser);
        }
    }

    private static object ChannelNameOrId(long channelId)
    {
        if (ChannelsById.TryGetValue(channelId, out TwitchChannelDto? channel))
            return channel.ChannelName;

        return channelId;
    }

    private static string GetString(DateTime time) => "(created " + (DateTime.UtcNow - time.ToUniversalTime()) switch
    {
        { Days: > 0 } ts => $"{ts.Days}d, {ts.Hours}h ago)",
        { Hours: > 0 } ts => $"{ts.Hours}h, {ts.Minutes}m ago)",
        { Minutes: > 0 } ts => $"{ts.Minutes}m, {ts.Seconds}s ago)",
        _ => $"{(DateTime.UtcNow - time.ToUniversalTime()).Seconds}s ago)"
    };

    protected override async ValueTask OnModuleEnabled()
    {
        TwitchPubSub.OnFollow += OnFollow;
        AnonClient.OnUserBan += OnBan;
        MainClient.OnUserBan += OnBan;
        foreach (TwitchChannelDto channel in ChannelsSetup.Channels.Values.Where(c => c.SusCheck))
            await TwitchPubSub.ListenTo(Topics.Following(channel.ChannelId));
    }

    protected override async ValueTask OnModuleDisabled()
    {
        TwitchPubSub.OnFollow -= OnFollow;
        AnonClient.OnUserBan -= OnBan;
        MainClient.OnUserBan -= OnBan;
        foreach (TwitchChannelDto channel in ChannelsSetup.Channels.Values.Where(c => c.SusCheck))
            await TwitchPubSub.UnlistenTo(Topics.Following(channel.ChannelId));
    }

    private readonly record struct SuspiciousUser(long UserId, long ChannelId);
}