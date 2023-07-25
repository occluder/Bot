using System.Text;
using Bot.Interfaces;
using Bot.Utils;
using MiniTwitch.PubSub.Interfaces;
using MiniTwitch.PubSub.Models;
using MiniTwitch.PubSub.Models.Payloads;

namespace Bot.Modules;

internal class PredictionNotifications : IModule
{
    public bool Enabled { get; private set; }

    private readonly HttpClient _requests = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly string _link = Config.Links["PredictionNotifications"];
    private readonly Dictionary<string, string> _emotes = new()
    {
        { "blue-1", "<:blue1:1132358378191061254>" }, { "blue-2", "<:blue2:1132358379826860063" }, { "blue-3", "<:blue3:1132358383027093634>" },
        { "blue-4", "<:blue4:1132358386277695609>" }, { "blue-5", "<:blue5:1132358388207075349>" }, { "blue-6", "<:blue6:1132358416485077002>" },
        { "blue-7", "<:blue7:1132358419802771526>" }, { "blue-8", "<:blue8:1132358421505638523>" }, { "blue-9", "<:blue9:1132358424571682898>" },
        { "blue-10", "<:blue10:1132358426203263037>" }, { "gray-1", "<:gray1:1132358473837977761>" }, { "gray-2", "<:gray2:1132358475557654588>" },
        { "pink-1", "<:pink1:1132358478653038617>" }, { "pink-2", "<:pink2:1132358481719070752>" }
    };

    private async ValueTask OnPredictionStarted(ChannelId channelId, IPredictionStarted prediction)
    {
        var builder = new DiscordMessageBuilder().AddEmbed(embed =>
        {
            embed.title = "Prediction Started!";
            embed.description = $"{prediction.Title}\n" + string.Join('\n', prediction.Outcomes.Select(x => _emotes[x.Badge.Version] + ' ' + x.Title));
            embed.timestamp = prediction.CreatedAt;
            embed.color = 5766924;
            embed.SetAuthor(author =>
            {
                author.name = ChannelsById[channelId].DisplayName;
                author.icon_url = ChannelsById[channelId].AvatarUrl;
                author.url = $"https://www.twitch.tv/popout/{ChannelsById[channelId].Username}/chat?popout=";
            });

            embed.SetFooter(footer =>
            {
                footer.text = $"Prediction started by {prediction.CreatedBy.UserDisplayName} • Closes in {prediction.PredictionWindowSeconds} seconds";
            });
        });

        await SendMessage(builder);
    }
    private async ValueTask OnPredictionLocked(ChannelId channelId, IPredictionLocked prediction)
    {
        var builder = new DiscordMessageBuilder().AddEmbed(embed =>
        {
            embed.title = "Prediction Locked!";
            embed.description = prediction.Title;
            embed.timestamp = prediction.LockedAt!.Value;
            embed.color = 6298368;
            embed.SetAuthor(author =>
            {
                author.name = ChannelsById[channelId].DisplayName;
                author.icon_url = ChannelsById[channelId].AvatarUrl;
            });

            embed.SetFooter(footer =>
            {
                footer.text = $"Prediction locked by {prediction.LockedBy?.UserDisplayName}";
            });

            foreach (var outcome in prediction.Outcomes)
            {
                embed.AddField(feed =>
                {
                    feed.name = _emotes[outcome.Badge.Version] + ' ' + outcome.Title;
                    feed.value = GetOutcomeData(outcome, prediction.Outcomes);
                    feed.inline = prediction.Outcomes.Count > 2;
                });
            }
        });

        await SendMessage(builder);
    }
    private async ValueTask OnPredictionWindowClosed(ChannelId channelId, IPredictionWindowClosed prediction)
    {
        var builder = new DiscordMessageBuilder().AddEmbed(embed =>
        {
            embed.title = "Prediction Closed!";
            embed.description = prediction.Title;
            embed.timestamp = prediction.CreatedAt!.AddSeconds(prediction.PredictionWindowSeconds);
            embed.color = 6298368;
            embed.SetAuthor(author =>
            {
                author.name = ChannelsById[channelId].DisplayName;
                author.icon_url = ChannelsById[channelId].AvatarUrl;
            });

            embed.SetFooter(footer =>
            {
                footer.text = $"Prediction started by {prediction.CreatedBy.UserDisplayName}";
            });

            foreach (var outcome in prediction.Outcomes)
            {
                embed.AddField(feed =>
                {
                    feed.name = _emotes[outcome.Badge.Version] + ' ' + outcome.Title;
                    feed.value = GetOutcomeData(outcome, prediction.Outcomes);
                    feed.inline = prediction.Outcomes.Count > 2;
                });
            }
        });

        await SendMessage(builder);
    }
    private async ValueTask OnPredictionCancelled(ChannelId channelId, IPredictionCancelled prediction)
    {
        var builder = new DiscordMessageBuilder().AddEmbed(embed =>
        {
            embed.title = "Prediction Cancelled!";
            embed.description = prediction.Title;
            embed.timestamp = prediction.EndedAt!.Value;
            embed.color = 13614414;
            embed.SetAuthor(author =>
            {
                author.name = ChannelsById[channelId].DisplayName;
                author.icon_url = ChannelsById[channelId].AvatarUrl;
            });

            embed.SetFooter(footer =>
            {
                footer.text = $"Prediction cancelled by {prediction.EndedBy?.UserDisplayName}";
            });

            foreach (var outcome in prediction.Outcomes)
            {
                embed.AddField(feed =>
                {
                    feed.name = _emotes[outcome.Badge.Version] + ' ' + outcome.Title;
                    feed.value = GetOutcomeData(outcome, prediction.Outcomes);
                    feed.inline = prediction.Outcomes.Count > 2;
                });
            }
        });

        await SendMessage(builder);
    }
    private async ValueTask OnPredictionEnded(ChannelId channelId, IPredictionEnded prediction)
    {
        var builder = new DiscordMessageBuilder().AddEmbed(embed =>
        {
            embed.title = "Prediction Ended!";
            embed.description = prediction.Title;
            embed.timestamp = prediction.EndedAt!.Value;
            embed.color = 7053553;
            embed.SetAuthor(author =>
            {
                author.name = ChannelsById[channelId].DisplayName;
                author.icon_url = ChannelsById[channelId].AvatarUrl;
            });

            embed.SetFooter(footer =>
            {
                footer.text = $"Prediction ended by {prediction.EndedBy?.UserDisplayName}";
            });

            foreach (var outcome in prediction.Outcomes)
            {
                embed.AddField(feed =>
                {
                    feed.name = _emotes[outcome.Badge.Version] + ' ' + outcome.Title;
                    feed.value = GetOutcomeData(outcome, prediction.Outcomes);
                    feed.inline = prediction.Outcomes.Count > 2;
                });
            }

            var winningOutcome = prediction.Outcomes.FirstOrDefault(x => x.Id == prediction.WinningOutcomeId);
            embed.AddField(feed =>
            {
                feed.name = $"Winning outcome: *{winningOutcome.Title}*";
                feed.value = string.Join('\n', winningOutcome.TopPredictors.Select(p => $"**@{p.UserDisplayName}**\t+{p.Result!.Value.PointsWon}"));
            });
        });

        await SendMessage(builder);
    }

    private static string GetOutcomeData(ChannelPredictions.Outcome outcome, IReadOnlyList<ChannelPredictions.Outcome> outcomes)
    {
        int allUsers = outcomes.Sum(x => x.TotalUsers);
        int allPoints = outcomes.Sum(x => x.TotalPoints);
        StringBuilder sb = new();
        sb.Append("👥 ");
        if (outcome.TotalUsers > 1000)
            sb.Append($"{Math.Round(outcome.TotalUsers / (double)1000, 1)}k users chose this outcome");
        else
            sb.Append($"{outcome.TotalUsers} users chose this outcome");

        sb.AppendLine($" ({100 * outcome.TotalUsers / allUsers}%)");
        sb.Append("💰 ");
        if (outcome.TotalPoints > 1_000_000)
            sb.Append($"{Math.Round(outcome.TotalPoints / (double)1_000_000, 2)}M points");
        else if (outcome.TotalPoints > 1000)
            sb.Append($"{Math.Round(outcome.TotalPoints / (double)1000, 1)}K points");
        else
            sb.Append($"{outcome.TotalPoints} points");

        sb.AppendLine($" ({100L * outcome.TotalPoints / allPoints}%)");
        return sb.ToString();
    }

    private async Task SendMessage(DiscordMessageBuilder builder)
    {
        HttpResponseMessage response;
        try
        {
            response = await _requests.PostAsync(_link, builder.ToStringContent());
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

    public async ValueTask Enable()
    {
        if (this.Enabled)
            return;

        foreach (string channel in Config.PredictionChannels)
            _ = await TwitchPubSub.ListenTo(Topics.ChannelPredictions(Channels[channel].Id));

        TwitchPubSub.OnPredictionStarted += OnPredictionStarted;
        TwitchPubSub.OnPredictionLocked += OnPredictionLocked;
        TwitchPubSub.OnPredictionWindowClosed += OnPredictionWindowClosed;
        TwitchPubSub.OnPredictionCancelled += OnPredictionCancelled;
        TwitchPubSub.OnPredictionEnded += OnPredictionEnded;
        this.Enabled = true;
        await Settings.EnableModule(nameof(PredictionNotifications));
    }

    public async ValueTask Disable()
    {
        if (!this.Enabled)
            return;

        foreach (string channel in Config.PredictionChannels)
            _ = await TwitchPubSub.UnlistenTo(Topics.ChannelPredictions(Channels[channel].Id));

        TwitchPubSub.OnPredictionStarted -= OnPredictionStarted;
        TwitchPubSub.OnPredictionLocked -= OnPredictionLocked;
        TwitchPubSub.OnPredictionWindowClosed -= OnPredictionWindowClosed;
        TwitchPubSub.OnPredictionCancelled -= OnPredictionCancelled;
        TwitchPubSub.OnPredictionEnded -= OnPredictionEnded;
        this.Enabled = false;
        await Settings.DisableModule(nameof(PredictionNotifications));
    }
}
