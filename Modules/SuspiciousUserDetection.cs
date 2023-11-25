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
    private const int SUS_MAX_AGE_DAYS = 1;
    private const int MAX_MONITOR_DAYS = 90;
    
    private static readonly HashSet<SuspiciousUser> _suspiciousUserIds = SuspiciousUsers.ToHashSet();
    private static readonly ILogger _logger = ForContext<SuspiciousUserDetection>();
    private static long _highestNonSuspiciousId;

    private static IRedisSet<SuspiciousUser> SuspiciousUsers =>
        Collections.GetRedisSet<SuspiciousUser>("bot:chat:suspicious_users");


    private static async ValueTask OnFollow(ChannelId channelId, Follower follower)
    {
        if (follower.Id < _highestNonSuspiciousId) return;
        HelixResult<Users> result = await HelixClient.GetUsers(follower.Id);
        if (result is { Success: false })
        {
            _logger.Error("Failed to get user with id {Id}: {@HelixResult}", follower.Id, result);
            return;
        }

        DateTime createdAt = result.Value.Data[0].CreatedAt.ToUniversalTime();
        // User is added
        if (createdAt > DateTime.UtcNow.AddDays(-SUS_MAX_AGE_DAYS))
        {
            await AddSuspiciousUser(follower.Id, channelId);
            _logger.Information("New sus user: {Username}, #{ChannelId}", follower.Name, ChannelNameOrId(channelId));
            return;
        }

        if (_highestNonSuspiciousId < follower.Id) _highestNonSuspiciousId = follower.Id;
        _logger.Information("Updated highest non-suspicious id to {NewId}", follower.Id);
        await Cleanup();
    }

    private static async ValueTask OnBan(IUserBan arg)
    {
        var user = new SuspiciousUser(arg.Target.Id, arg.Channel.Id);
        if (!_suspiciousUserIds.Contains(user)) return;
        _suspiciousUserIds.Remove(user);
        await SuspiciousUsers.RemoveAsync(user);
        _logger.Information(
            "Suspicious user {SuspiciousUser} banned from #{Channel}",
            arg.Target.Name,
            arg.Channel.Name
        );

        _ = await HelixClient.UpdateUserChatColor(ChatColor.HotPink);
        await MainClient.SendMessage(
            Config.Secrets["ParentUsername"],
            $"AOLpls @{arg.Target.Name} banned from #{arg.Channel.Name}",
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
        // Get lowest 100 user IDs
        HelixResult<Users> usersResult = await HelixClient.GetUsers(
            _suspiciousUserIds.Select(x => x.UserId).Order().Take(100)
        );

        if (!usersResult.Success) return;
        // Go over users by order of IDs to stop early
        foreach (SuspiciousUser suspiciousUser in _suspiciousUserIds.OrderBy(x => x.UserId))
        {
            Users.User? helixUser = usersResult.Value.Data.FirstOrDefault(u => u.Id == suspiciousUser.ChannelId);
            if (helixUser is null) continue;

            // If the current user is within monitored threshold, then we can stop early because of order
            if (helixUser.CreatedAt >= DateTime.UtcNow.AddDays(-MAX_MONITOR_DAYS)) break;
            // Remove user otherwise
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