global using static Bot.Workflows.ChannelsSetup;
using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;

namespace Bot.Workflows;

internal class ChannelsSetup : IWorkflow
{
    public static Dictionary<string, TwitchChannelDto> Channels { get; } = new();
    public static Dictionary<long, TwitchChannelDto> ChannelsById { get; } = new();

    public async ValueTask<WorkflowState> Run()
    {
        TwitchChannelDto[] channels;
        try
        {
            channels = (await Postgres.QueryAsync<TwitchChannelDto>("select * from channels", commandTimeout: 5)).ToArray();
        }
        catch (Exception ex)
        {
            ForContext<ChannelsSetup>().Fatal(ex, "{ClassName} Failed to setup channels");
            return WorkflowState.Failed;
        }

        foreach (TwitchChannelDto channel in channels)
        {
            Channels.Add(channel.Username, channel);
            ChannelsById.Add(channel.Id, channel);
        }

        Information("{ChannelCount} Channels loaded", channels.Length);
        Information("Joining MainClient channels");
        bool success = await MainClient.JoinChannels(channels.Where(x => x.Priority >= 50).Select(x => x.Username));
        if (!success)
            Warning("MainClient failed to join some channels");
        else
            Information("MainClient finished joining Channels");

        Information("Joining AnonClient channels");
        bool success2 = await AnonClient.JoinChannels(channels.Where(x => x.Priority is < 50 and > -10).Select(x => x.Username));
        if (!success2)
            Warning("AnonClient failed to join some channels");
        else
            Information("AnonClient finished joining Channels");

        return WorkflowState.Completed;
    }
}