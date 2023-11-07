using Bot.Models;
using CachingFramework.Redis.Contracts.RedisObjects;
using MiniTwitch.Irc.Interfaces;
using MiniTwitch.PubSub.Models;
using MiniTwitch.PubSub.Payloads;

namespace Bot.Modules;

public class SuspiciousUserDetection: BotModule
{
    private static readonly HashSet<SuspiciousUser> _suspiciousUserIds = SuspiciousUsers.ToHashSet();
    private static readonly ILogger _logger = ForContext<SuspiciousUserDetection>();
    private static long _highestNonSuspiciousId;

    private static IRedisSet<SuspiciousUser> SuspiciousUsers =>
        Collections.GetRedisSet<SuspiciousUser>("bot:chat:suspicious_users");

    private static async ValueTask OnFollow(ChannelId channelId, Follower follower)
    {
        if (follower.Id < _highestNonSuspiciousId) return;
        if (await HelixClient.GetUsers(follower.Id) is { Success: true } result)
        {
            if (result.Value.Data[0].CreatedAt.ToUniversalTime() > DateTime.UtcNow.AddDays(-1))
            {
                await AddSuspiciousUser(follower.Id, channelId);
                await MainClient.SendMessage(
                    Config.RelayChannel,
                    $"\u26a0\ufe0f Suspicious user detected: {follower.Name} in #{ChannelNameOrId(channelId)}"
                );

                _logger.Information("Suspicious user detected: {UserId}, #{ChannelId}", follower.Id, channelId);
                return;
            }

            if (_highestNonSuspiciousId < follower.Id) _highestNonSuspiciousId = follower.Id;
            _logger.Information("Updated highest non-suspicious id to {NewId}", follower.Id);
            await Cleanup();
            return;
        }

        _logger.Error("Failed to get user with id {Id}: {@HelixResult}", follower.Id, result);
    }

    private static ValueTask OnBan(IUserBan arg)
    {
        var user = new SuspiciousUser(arg.Target.Id, arg.Channel.Id);
        if (!_suspiciousUserIds.Contains(user)) return default;
        _suspiciousUserIds.Remove(user);
        SuspiciousUsers.Remove(user);
        _logger.Information(
            "Suspicious user {SuspiciousUser} banned from #{Channel}",
            arg.Target.Name,
            arg.Channel.Name
        );
        
        return MainClient.SendMessage(
            Config.RelayChannel,
            $"ℹ\ufe0f Suspicious user {arg.Target.Name} has been banned from #{arg.Channel.Name}"
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

    protected override async ValueTask OnModuleEnabled()
    {
        TwitchPubSub.OnFollow += OnFollow;
        AnonClient.OnUserBan += OnBan;
        MainClient.OnUserBan += OnBan;
        foreach (TwitchChannelDto channel in Channels.Values.Where(c => c.SusCheck))
            await TwitchPubSub.ListenTo(Topics.Following(channel.ChannelId));
    }

    protected override async ValueTask OnModuleDisabled()
    {
        TwitchPubSub.OnFollow -= OnFollow;
        AnonClient.OnUserBan -= OnBan;
        MainClient.OnUserBan -= OnBan;
        foreach (TwitchChannelDto channel in Channels.Values.Where(c => c.SusCheck))
            await TwitchPubSub.UnlistenTo(Topics.Following(channel.ChannelId));
    }

    private readonly record struct SuspiciousUser(long UserId, long ChannelId);
}