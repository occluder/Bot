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

    public static async Task JoinChannel(IvrUser user, int priority, bool isLogged)
    {
        await PostgresTimerSemaphore.WaitAsync();
        try
        {
            TwitchChannelDto channelDto = new()
            {
                DisplayName = user.DisplayName,
                Username = user.Login,
                Id = long.Parse(user.Id),
                AvatarUrl = user.Logo,
                Priority = priority,
                IsLogged = isLogged,
                DateJoined = DateTime.Now,
            };

            _ = await Postgres.ExecuteAsync("insert into channels values (@DisplayName, @Username, @Id, @AvatarUrl, @Priority, @IsLogged, @DateJoined)", channelDto);
            if (priority >= 50)
                await MainClient.JoinChannel(user.Login);
            else
                await AnonClient.JoinChannel(user.Login);

            Channels[channelDto.Username] = channelDto;
            ChannelsById[channelDto.Id] = channelDto;
        }
        finally
        {
            _ = PostgresTimerSemaphore.Release();
        }
    }

    public static async Task PartChannel(long channelId)
    {
        await PostgresTimerSemaphore.WaitAsync();
        try
        {
            _ = await Postgres.ExecuteAsync("delete from channels where id = @ChannelId", new { ChannelId = channelId });
            TwitchChannelDto channelDto = ChannelsById[channelId];
            Channels.Remove(channelDto.Username);
            ChannelsById.Remove(channelDto.Id);
        }
        finally
        {
            _ = PostgresTimerSemaphore.Release();
        }
    }
}