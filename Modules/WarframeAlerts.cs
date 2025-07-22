using System.Text.Json;
using Bot.Models;
using Bot.Utils;
using CachingFramework.Redis.Contracts.RedisObjects;
using MiniTwitch.PubSub.Interfaces;
using MiniTwitch.PubSub.Models;

namespace Bot.Modules;

public class WarframeAlerts: BotModule
{
    static readonly string[] _items = ["orokin catalyst", "orokin reactor", "forma", "exilus"];
    static IRedisSet<string> Ids => Collections.GetRedisSet<string>("warframe:data:worldstate_ids");
    static string[] _channels = [];
    static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    readonly BackgroundTimer _timer;

    public WarframeAlerts()
    {
        _timer = new(TimeSpan.FromMinutes(10), CheckWorldstate);
    }

    static async Task CheckWorldstate()
    {
        if (_channels.Length == 0)
        {
            using var db = await NewDbConnection();
            _channels = await db.QuerySingleAsync<string[]>("SELECT value FROM persistent_object WHERE key = 'warframe_alert_channels'");
        }
        await CheckAlerts();
        await CheckInvasions();
    }

    static async Task CheckAlerts()
    {
        var alertReq = await GetFromRequest<Alert[]>("https://api.warframestat.us/pc/alerts?language=en", _options);
        if (!alertReq.IsT0)
        {
            return;
        }

        foreach (Alert alert in alertReq.AsT0)
        {
            if (!alert.Active || !await HandleId(alert.Id))
            {
                continue;
            }

            string missionName = alert.Mission.NodeKey;
            int minLevel = alert.Mission.MinEnemyLevel;
            int maxLevel = alert.Mission.MaxEnemyLevel;
            string rewardStr = alert.Mission.Reward.AsString;
            string lower = rewardStr.ToLower();
            if (_items.Any(lower.Contains))
            {
                await MainClient.SendMessage(_channels, $"@warframers pajaDink 🚨 {rewardStr} alert on {missionName} ({minLevel}-{maxLevel})");
            }
        }
    }

    static async Task CheckInvasions()
    {
        var invasionReq = await GetFromRequest<Invasion[]>("https://api.warframestat.us/pc/invasions?language=en", _options);
        if (!invasionReq.IsT0)
        {
            return;
        }

        foreach (Invasion invasion in invasionReq.AsT0)
        {
            if (invasion.Completed || !await HandleId(invasion.Id))
            {
                continue;
            }

            string rewardStr = $"[{invasion.Attacker.Reward?.AsString}] vs [{invasion.Defender.Reward?.AsString}]";
            string lower = rewardStr.ToLower();
            if (_items.Any(lower.Contains))
            {
                await MainClient.SendMessage(_channels, $"@warframers pajaDink 🚨 {rewardStr} invasion on {invasion.NodeKey}");
            }
        }
    }

    static async Task<bool> HandleId(string id)
    {
        if (await Ids.ContainsAsync(id))
        {
            return false;
        }

        await Ids.AddAsync(id);
        return true;
    }
    static async ValueTask OnStreamUp(ChannelId channelId, IStreamUp _)
    {
        if (await HelixClient.GetChannelInformation(channelId) is not { Success: true } result)
        {
            return;
        }

        string title = result.Value.Data[0].Title;
        string game = result.Value.Data[0].GameName;
        if (channelId == 31557216
            && title.Contains("devstream", StringComparison.OrdinalIgnoreCase))
        {
            await MainClient.SendMessage(
            _channels,
                $"@warframers pajaDink Now live: {title} [{game}]",
                result.Success
            );
        }
    }

    protected override ValueTask OnModuleEnabled()
    {
        TwitchPubSub.OnStreamUp += OnStreamUp;
        _timer.Start();
        return default;
    }

    protected override async ValueTask OnModuleDisabled()
    {
        TwitchPubSub.OnStreamUp -= OnStreamUp;
        await _timer.StopAsync();
    }
}
