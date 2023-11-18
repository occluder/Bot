using System.Net.Http.Json;
using System.Text;
using Bot.Models;
using MiniTwitch.PubSub.Interfaces;
using MiniTwitch.PubSub.Models;
using MiniTwitch.PubSub.Payloads;

namespace Bot.Modules;

internal class PredictionNotifications: BotModule
{
    private readonly HttpClient _requests = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly string _link = Config.Links["PredictionNotifications"];
    private readonly Dictionary<string, string> _emotes = new()
    {
        { "blue-1", "<:blue1:1132358378191061254>" }, { "blue-2", "<:blue2:1132358379826860063>" }, { "blue-3", "<:blue3:1132358383027093634>" },
        { "blue-4", "<:blue4:1132358386277695609>" }, { "blue-5", "<:blue5:1132358388207075349>" }, { "blue-6", "<:blue6:1132358416485077002>" },
        { "blue-7", "<:blue7:1132358419802771526>" }, { "blue-8", "<:blue8:1132358421505638523>" }, { "blue-9", "<:blue9:1132358424571682898>" },
        { "blue-10", "<:blue10:1132358426203263037>" }, { "gray-1", "<:gray1:1132358473837977761>" }, { "gray-2", "<:gray2:1132358475557654588>" },
        { "pink-1", "<:pink1:1132358478653038617>" }, { "pink-2", "<:pink2:1132358481719070752>" }
    };

    private async ValueTask OnPredictionStarted(ChannelId channelId, IPredictionStarted prediction)
    {
        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = "Prediction Started!",
                    description = $"{prediction.Title}\n" + string.Join('\n',
                        prediction.Outcomes.Select(x => _emotes[x.Badge.Version] + ' ' + x.Title)),
                    timestamp = prediction.CreatedAt,
                    color = 5766924,
                    author = new
                    {
                        name = ChannelsById[channelId].DisplayName,
                        icon_url = ChannelsById[channelId].AvatarUrl,
                        url = $"https://www.twitch.tv/popout/{ChannelsById[channelId].ChannelName}/chat?popout="
                    },
                    footer = new
                    {
                        text = $"Prediction started by {prediction.CreatedBy.DisplayName} " +
                               $"• Closes in {(prediction.PredictionWindowSeconds > 120
                                   ? (prediction.PredictionWindowSeconds / 60) + "m"
                                   : prediction.PredictionWindowSeconds + "s")}"
                    }
                }
            }
        };

        await SendMessage(payload);
    }
    private async ValueTask OnPredictionLocked(ChannelId channelId, IPredictionLocked prediction)
    {
        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = "Prediction Locked!",
                    description = prediction.Title,
                    timestamp = prediction.LockedAt!.Value,
                    color = 6298368,
                    author = new
                    {
                        name = ChannelsById[channelId].DisplayName,
                        icon_url = ChannelsById[channelId].AvatarUrl
                    },
                    footer = new
                    {
                        text = $"Prediction locked by {prediction.LockedBy?.DisplayName}"
                    },
                    fields = prediction.Outcomes.Select(x => new
                    {
                        name = _emotes[x.Badge.Version] + ' ' + x.Title,
                        value = GetOutcomeData(x, prediction.Outcomes),
                        inline = prediction.Outcomes.Count > 2
                    })
                }
            }
        };

        await SendMessage(payload);
    }
    private async ValueTask OnPredictionWindowClosed(ChannelId channelId, IPredictionWindowClosed prediction)
    {
        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = "Prediction Closed!",
                    description = prediction.Title,
                    timestamp = prediction.CreatedAt!.AddSeconds(prediction.PredictionWindowSeconds),
                    color = 6298368,
                    author = new
                    {
                        name = ChannelsById[channelId].DisplayName,
                        icon_url = ChannelsById[channelId].AvatarUrl
                    },
                    footer = new
                    {
                        text = $"Prediction started by {prediction.CreatedBy.DisplayName}"
                    },
                    fields = prediction.Outcomes.Select(x => new
                    {
                        name = _emotes[x.Badge.Version] + ' ' + x.Title,
                        value = GetOutcomeData(x, prediction.Outcomes),
                        inline = prediction.Outcomes.Count > 2
                    })
                }
            }
        };

        await SendMessage(payload);
    }
    private async ValueTask OnPredictionCancelled(ChannelId channelId, IPredictionCancelled prediction)
    {
        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = "Prediction Cancelled!",
                    description = prediction.Title,
                    timestamp = prediction.EndedAt!.Value,
                    color = 13614414,
                    author = new
                    {
                        name = ChannelsById[channelId].DisplayName,
                        icon_url = ChannelsById[channelId].AvatarUrl
                    },
                    footer = new
                    {
                        text = $"Prediction cancelled by {prediction.EndedBy?.DisplayName}"
                    },
                    fields = prediction.Outcomes.Select(x => new
                    {
                        name = _emotes[x.Badge.Version] + ' ' + x.Title,
                        value = GetOutcomeData(x, prediction.Outcomes),
                        inline = prediction.Outcomes.Count > 2
                    })
                }
            }
        };

        await SendMessage(payload);
    }
    private async ValueTask OnPredictionEnded(ChannelId channelId, IPredictionEnded prediction)
    {
        ChannelPredictions.Outcome win = prediction.Outcomes.FirstOrDefault(x => x.Id == prediction.WinningOutcomeId);
        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = "Prediction Ended!",
                    description = prediction.Title,
                    timestamp = prediction.EndedAt!.Value,
                    color = 7053553,
                    author = new
                    {
                        name = ChannelsById[channelId].DisplayName,
                        icon_url = ChannelsById[channelId].AvatarUrl
                    },
                    footer = new
                    {
                        text = $"Prediction ended by {prediction.EndedBy?.DisplayName}"
                    },
                    fields = prediction.Outcomes.Select(x => new
                    {
                        name = _emotes[x.Badge.Version] + ' ' + x.Title,
                        value = GetOutcomeData(x, prediction.Outcomes),
                        inline = prediction.Outcomes.Count > 2
                    }).Append(new
                    {
                        name = $"🏆 Winning outcome: *{win.Title}*",
                        value = string.Join('\n',
                            win.TopPredictors.Select(p => $"**@{p.DisplayName}**\t+{p.Result!.Value.PointsWon}")),
                        inline = false
                    })
                }
            }
        };

        await SendMessage(payload);
    }

    private static string GetOutcomeData(ChannelPredictions.Outcome outcome, IReadOnlyList<ChannelPredictions.Outcome> outcomes)
    {
        int allUsers = outcomes.Sum(x => x.TotalUsers);
        int allPoints = outcomes.Sum(x => x.TotalPoints);
        StringBuilder sb = new();
        _ = sb.Append("👥 ");
        _ = outcome.TotalUsers > 1000
            ? sb.Append($"{Math.Round(outcome.TotalUsers / (double)1000, 1)}k users chose this outcome")
            : sb.Append($"{outcome.TotalUsers} users chose this outcome");

        _ = sb.AppendLine($" ({100 * outcome.TotalUsers / allUsers}%)");
        _ = sb.Append("💰 ");
        _ = outcome.TotalPoints > 1_000_000
            ? sb.Append($"{Math.Round(outcome.TotalPoints / (double)1_000_000, 2)}M points")
            : outcome.TotalPoints > 1000
            ? sb.Append($"{Math.Round(outcome.TotalPoints / (double)1000, 1)}K points")
            : sb.Append($"{outcome.TotalPoints} points");

        _ = sb.AppendLine($" ({100L * outcome.TotalPoints / allPoints}%)");
        return sb.ToString();
    }

    private async Task SendMessage(object payload)
    {
        try
        {
            HttpResponseMessage response = await _requests.PostAsJsonAsync(_link, payload);
            if (response.IsSuccessStatusCode)
                ForContext<PredictionNotifications>().Debug("[{StatusCode}] POST {Url}", response.StatusCode, _link);
            else
                ForContext<PredictionNotifications>().Warning("[{StatusCode}] POST {Url}", response.StatusCode, _link);
        }
        catch (Exception ex)
        {
            ForContext<PredictionNotifications>().Error(ex, "POST {Url}", _link);
        }
    }

    protected override async ValueTask OnModuleEnabled()
    {
        foreach (TwitchChannelDto? channel in Channels.Values.Where(c => c.PredictionsEnabled))
            _ = await TwitchPubSub.ListenTo(Topics.ChannelPredictions(channel.ChannelId));

        TwitchPubSub.OnPredictionStarted += OnPredictionStarted;
        TwitchPubSub.OnPredictionLocked += OnPredictionLocked;
        TwitchPubSub.OnPredictionWindowClosed += OnPredictionWindowClosed;
        TwitchPubSub.OnPredictionCancelled += OnPredictionCancelled;
        TwitchPubSub.OnPredictionEnded += OnPredictionEnded;
    }
    protected override async ValueTask OnModuleDisabled()
    {
        foreach (TwitchChannelDto? channel in Channels.Values.Where(c => c.PredictionsEnabled))
            _ = await TwitchPubSub.UnlistenTo(Topics.ChannelPredictions(channel.ChannelId));

        TwitchPubSub.OnPredictionStarted -= OnPredictionStarted;
        TwitchPubSub.OnPredictionLocked -= OnPredictionLocked;
        TwitchPubSub.OnPredictionWindowClosed -= OnPredictionWindowClosed;
        TwitchPubSub.OnPredictionCancelled -= OnPredictionCancelled;
        TwitchPubSub.OnPredictionEnded -= OnPredictionEnded;
    }
}
