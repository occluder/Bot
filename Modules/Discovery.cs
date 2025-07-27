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
            target = null;
        }

        if (target is null && (target = await GetTarget()) is null)
        {
            Debug("No discovery target set, Setting new.");
            target = new DiscoveryTarget(
                raid.Author.Name,
                raid.Author.Id,
                raid.ViewerCount,
                DateTimeOffset.Now.AddDays(7).ToUnixTimeMilliseconds()
            );
            await SaveTarget(target);
        }

        if (raid.ViewerCount < target.Viewers)
        {
            return;
        }

        if (AnonClient.JoinedChannels.Contains(raid.Author) || MainClient.JoinedChannels.Contains(raid.Author))
        {
            return;
        }

        if (AnonClient.JoinedChannels.Any(x => x.Id == target.ChannelId))
        {
            await PartChannel(target.ChannelId);
        }

        var response = await GetFromRequest<IvrUser[]>($"https://api.ivr.fi/v2/twitch/user?login={raid.Author.Name}");
        if (response.TryPickT0(out IvrUser[] users, out _))
        {
            await JoinChannel(users[0], -1, true);
        }

        Information(
            "Discovery target changed from {OldChannel} to {Channel} with {Viewers} viewers until {StopAt}.",
            target.Channel,
            raid.Author.Name,
            raid.ViewerCount,
            DateTimeOffset.FromUnixTimeMilliseconds(target.StopAt)
        );

        target = target with
        {
            Channel = raid.Author.Name,
            ChannelId = raid.Author.Id,
            Viewers = raid.ViewerCount
        };
    }

    static async Task SaveTarget(DiscoveryTarget hop)
    {
        using var db = await NewDbConnection();
        try
        {
            await db.ExecuteAsync(
                """
                INSERT INTO persistent_object VALUES (@Key, @Value)
                ON CONFLICT (key) DO UPDATE SET persistent_object.value = @Value::jsonb
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

    protected override ValueTask OnModuleEnabled()
    {
        MainClient.OnRaidNotice += OnRaid;
        AnonClient.OnRaidNotice += OnRaid;
        return default;
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
