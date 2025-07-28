using Bot.Models;
using MiniTwitch.Irc.Interfaces;

namespace Bot.Modules;

public class Discovery: BotModule
{
    DiscoveryTarget? target;
    const string DISCOVERY_KEY = "discovery_target";

    private async ValueTask OnRaid(IRaidNotice raid)
    {
        if (target is not null && target.StopAt < UnixMs())
        {
            Debug("Current discovery target has expired, resetting.");
            await PartChannel(target.ChannelId);
            target = null;
        }

        if (target is null)
        {
            Debug("No discovery target set, Setting new.");
            target = new DiscoveryTarget(
                raid.Author.Name,
                raid.Author.Id,
                raid.ViewerCount,
                DateTimeOffset.Now.AddDays(7).ToUnixTimeMilliseconds()
            );
        }

        if (raid.ViewerCount < target.Viewers)
        {
            return;
        }

        if (Channels.ContainsKey(raid.Author.Name))
        {
            return;
        }

        // This is not raid author, we're leaving the current target channel
        if (ChannelsById.ContainsKey(target.ChannelId))
        {
            await PartChannel(target.ChannelId);
        }

        await JoinChannel(await GetUser(raid.Author.Name), -1, true);
        Information(
            "Discovery target changed from {OldChannel} to {Channel} with {Viewers} viewers. Expires: {TimeLeft}.",
            target.Channel,
            raid.Author.Name,
            raid.ViewerCount,
            PrettyTimeString(DateTimeOffset.FromUnixTimeMilliseconds(target.StopAt) - DateTimeOffset.UtcNow)
        );

        target = target with
        {
            Channel = raid.Author.Name,
            ChannelId = raid.Author.Id,
            Viewers = raid.ViewerCount
        };
        await SaveTarget(target);
    }

    static async Task SaveTarget(DiscoveryTarget hop)
    {
        using var db = await NewDbConnection();
        try
        {
            await db.ExecuteAsync(
                """
                INSERT INTO persistent_object VALUES (@Key, @Value::jsonb)
                ON CONFLICT (key) DO UPDATE SET value = excluded.value
                """, new
                {
                    Key = DISCOVERY_KEY,
                    Value = hop
                }
            );
        }
        catch (Exception ex)
        {
            Error(ex, "Failed to save discovery target");
        }
    }

    static async Task<DiscoveryTarget?> GetTarget()
    {
        using var db = await NewDbConnection();
        try
        {
            var target = await db.QuerySingleOrDefaultAsync<DiscoveryTarget>(
                """
                SELECT value FROM persistent_object WHERE key = @Key
                """, new
                {
                    Key = DISCOVERY_KEY
                }
            );

            if (target is null || UnixMs() >= target.StopAt)
            {
                Debug("No valid discovery target found or discovery target has expired.");
                return null;
            }

            return target;
        }
        catch (Exception ex)
        {
            Warning(ex, "Failed to get discovery target");
            return null;
        }
    }

    static async Task<IvrUser> GetUser(string channelName)
    {
        var response = await GetFromRequest<IvrUser[]>($"https://api.ivr.fi/v2/twitch/user?login={channelName}");
        if (response.TryPickT0(out IvrUser[] users, out _))
        {
            return users[0];
        }

        throw new Exception($"Failed to get user {channelName} from IVR API");
    }

    protected async override ValueTask OnModuleEnabled()
    {
        MainClient.OnRaidNotice += OnRaid;
        AnonClient.OnRaidNotice += OnRaid;
        target = await GetTarget();
        if (target is null || UnixMs() > target.StopAt)
        {
            target = null;
            return;
        }

        await JoinChannel(await GetUser(target.Channel), -1, true);
    }


    protected async override ValueTask OnModuleDisabled()
    {
        MainClient.OnRaidNotice -= OnRaid;
        AnonClient.OnRaidNotice -= OnRaid;
        if (target is not null)
        {
            await PartChannel(target.ChannelId);
        }
    }
}

public record DiscoveryTarget(string Channel, long ChannelId, int Viewers, long StopAt);
