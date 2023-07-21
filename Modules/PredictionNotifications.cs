using System.Text;
using Bot.Interfaces;
using Bot.Utils;
using MiniTwitch.PubSub.Interfaces;
using MiniTwitch.PubSub.Models;
using MiniTwitch.PubSub.Models.Payloads;

namespace Bot.Modules;

internal class PredictionNotifications : IModule
{
    private const string BLUE_EMOTE = "<:blue:1131951905929703455>";
    private const string PINK_EMOTE = "<:pink:1131951908089757808>";

    public bool Enabled { get; private set; }

    private readonly HttpClient _requests = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly string _link = Config.Links["PredictionNotifications"];

    private async ValueTask OnPredictionStarted(ChannelId channelId, IPredictionStarted prediction)
    {
        var builder = new DiscordMessageBuilder().AddEmbed(embed =>
        {
            embed.title = "Prediction Started!";
            embed.description = prediction.Title;
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

            foreach (var outcome in prediction.Outcomes)
            {
                embed.AddField(feed =>
                {
                    feed.name = GetOutcomeEmote(outcome);
                    feed.value = outcome.Title;
                    feed.inline = prediction.Outcomes.Count > 2;
                });
            }
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
                    feed.name = GetOutcomeEmote(outcome);
                    feed.value = GetOutcomeData(outcome, prediction.Outcomes.Sum(o => o.TotalUsers));
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
                    feed.name = GetOutcomeEmote(outcome);
                    feed.value = GetOutcomeData(outcome, prediction.Outcomes.Sum(o => o.TotalUsers));
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
                    feed.name = GetOutcomeEmote(outcome);
                    feed.value = GetOutcomeData(outcome, prediction.Outcomes.Sum(o => o.TotalUsers));
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
                footer.text = $"Prediction cancelled by {prediction.EndedBy?.UserDisplayName}";
            });

            foreach (var outcome in prediction.Outcomes)
            {
                embed.AddField(feed =>
                {
                    feed.name = GetOutcomeEmote(outcome);
                    feed.value = GetOutcomeData(outcome, prediction.Outcomes.Sum(o => o.TotalUsers));
                    feed.inline = prediction.Outcomes.Count > 2;
                });
            }

            var winningOutcome = prediction.Outcomes.FirstOrDefault(x => x.Id == prediction.WinningOutcomeId);
            embed.AddField(feed =>
            {
                feed.name = $"Winning outcome: *{winningOutcome.Title}*";
                feed.value = string.Join('\n', winningOutcome.TopPredictors.Select(p => $"*@{p.UserDisplayName}* +{p.Result!.Value.PointsWon}"));
            });
        });

        await SendMessage(builder);
    }

    private static string GetOutcomeEmote(ChannelPredictions.Outcome outcome) => outcome.Badge.Version[0] == 'p' ? PINK_EMOTE : BLUE_EMOTE;
    private static string GetOutcomeData(ChannelPredictions.Outcome outcome, int totalUsers)
    {
        StringBuilder sb = new();
        sb.AppendLine(outcome.Title);
        sb.Append("👥 ");
        if (outcome.TotalUsers > 1000)
            sb.AppendLine($"{Math.Round(outcome.TotalUsers / (double)1000, 1)} users chose this outcome ({100 * outcome.TotalUsers / (double)totalUsers}%)");
        else
            sb.AppendLine($"{outcome.TotalUsers} users chose this outcome ({Math.Floor(outcome.TotalUsers / (double)totalUsers)}%)");

        sb.Append("💰 ");
        if (outcome.TotalPoints > 1_000_000)
            sb.AppendLine($"{Math.Round(outcome.TotalPoints / (double)1_000_000, 2)}M points");
        else if (outcome.TotalPoints > 1000)
            sb.AppendLine($"{Math.Round(outcome.TotalPoints / (double)1000, 1)}K points");
        else
            sb.AppendLine($"{outcome.TotalPoints} points");

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
