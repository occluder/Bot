using System.Text.Json;
using Bot.Models;
using Bot.Utils;
using CachingFramework.Redis.Contracts.RedisObjects;

namespace Bot.Modules;

public class WarframeAlerts: BotModule
{
    private static string[] _items = ["orokin catalyst", "orokin reactor", "forma", "exilus"];
    private static IRedisSet<string> Ids => Collections.GetRedisSet<string>("warframe:data:worldstate_ids");
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly BackgroundTimer _timer;

    public WarframeAlerts()
    {
        _timer = new(TimeSpan.FromMinutes(5), CheckWorldstate);
    }

    private async Task CheckWorldstate()
    {
        await CheckAlerts();
        await CheckInvasions();
    }

    private async Task CheckAlerts()
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
                await MainClient.SendMessage("pajlada", $"@warframers pajaDink 🚨 {rewardStr} alert on {missionName} ({minLevel}-{maxLevel})");
            }
        }
    }

    private async Task CheckInvasions()
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
                await MainClient.SendMessage("pajlada", $"@warframers pajaDink 🚨 {rewardStr} invasion on {invasion.NodeKey}");
            }
        }
    }

    private static async Task<bool> HandleId(string id)
    {
        if (await Ids.ContainsAsync(id))
        {
            return false;
        }

        await Ids.AddAsync(id);
        return true;
    }

    protected override ValueTask OnModuleEnabled()
    {
        _timer.Start();
        return default;
    }

    protected override async ValueTask OnModuleDisabled()
    {
        await _timer.StopAsync();
    }
}
