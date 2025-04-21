using Bot.Models;
using MiniTwitch.Irc.Interfaces;

namespace Bot.Modules;

public class Raids: BotModule
{
    static readonly ILogger _logger = ForContext<Raids>();

    static async ValueTask OnRaid(IRaidNotice notice)
    {
        if (!ChannelsById[notice.Channel.Id].IsLogged)
        {
            return;
        }

        if (!await AddChannelInfo(notice.Channel.Id))
        {
            return;
        }

        using var conn = await NewDbConnection();
        try
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO
                    channel_raid
                VALUES (
                    @FromChannel,
                    @FromChannelId,
                    @ToChannel,
                    @ToChannelId,
                    @Viewers,
                    @TimeSent
                )
                """,
                new
                {
                    FromChannel = notice.Channel.Name,
                    FromChannelId = notice.Channel.Id,
                    ToChannel = notice.Channel.Name,
                    ToChannelId = notice.Channel.Id,
                    Viewers = notice.ViewerCount,
                    TimeSent = notice.SentTimestamp.ToUnixTimeSeconds()
                }
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to insert raid!\n\t{@Notice}", notice);
            return;
        }
    }

    static async Task<bool> AddChannelInfo(long channelId)
    {
        var req = await HelixClient.GetUsers(channelId);
        if (!req.Success)
        {
            _logger.Error("Failed to get channel info for raid: {Error} ({Status})", req.Message, req.StatusCode);
            return false;
        }

        if (req.Value.Data.Count == 0)
        {
            _logger.Error("Failed to get channel info for raid, response empty:\n\t{@Response}", req);
            return false;
        }

        var channel = req.Value.Data[0];
        using var conn = await NewDbConnection();
        try
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO 
                    channels 
                VALUES (
                    @DisplayName, 
                    @ChannelName, 
                    @ChannelId, 
                    @AvatarUrl, 
                    @Priority, 
                    @Tags, 
                    @DateAdded
                ) ON CONFLICT DO UPDATE SET
                    display_name = EXCLUDED.display_name,
                    channel_name = EXCLUDED.channel_name,
                    avatar_url = EXCLUDED.avatar_url
                """,
                new
                {
                    channel.DisplayName,
                    ChannelName = channel.Name,
                    ChannelId = channel.Id,
                    AvatarUrl = channel.ProfileImageUrl,
                    Priority = -99,
                    Tags = (string?)null,
                    DateAdded = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to insert channel!\n\t{@Request}", req);
            return false;
        }

        return true;
    }

    protected override ValueTask OnModuleEnabled()
    {
        MainClient.OnRaidNotice += OnRaid;
        AnonClient.OnRaidNotice += OnRaid;
        return default;
    }


    protected override ValueTask OnModuleDisabled()
    {
        MainClient.OnRaidNotice -= OnRaid;
        AnonClient.OnRaidNotice -= OnRaid;
        return default;
    }
}
