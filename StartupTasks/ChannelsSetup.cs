using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;
using Serilog.Events;

namespace Bot.StartupTasks;

internal class ChannelsSetup: IStartupTask
{
    public static Dictionary<string, TwitchChannelDto> Channels { get; } = new();
    public static Dictionary<long, TwitchChannelDto> ChannelsById { get; } = new();

    public async ValueTask<StartupTaskState> Run()
    {
        TwitchChannelDto[] channels;
        try
        {
            channels = (await Postgres.QueryAsync<TwitchChannelDto>("select * from channels", commandTimeout: 5)).ToArray();
        }
        catch (Exception ex)
        {
            ForContext<ChannelsSetup>().Fatal(ex, "{ClassName} Failed to setup channels");
            return StartupTaskState.Failed;
        }

        foreach (TwitchChannelDto channel in channels)
        {
            Channels.Add(channel.Username, channel);
            ChannelsById.Add(channel.Id, channel);
        }

        Information("{ChannelCount} channels loaded. {JoinableCount} joinable. {PrioritizedCount} prioritized",
            channels.Length, channels.Count(x => x.Priority > -10), channels.Count(x => x.Priority >= 50));

        Information("Joining MainClient channels");
        LogEventLevel ll = LoggerSetup.LogSwitch.MinimumLevel;
        LoggerSetup.LogSwitch.MinimumLevel = LogEventLevel.Warning;
        bool success = await MainClient.JoinChannels(channels.Where(x => x.Priority >= 50).Select(x => x.Username));
        LoggerSetup.LogSwitch.MinimumLevel = ll;
        if (!success)
            Warning("MainClient failed to join some channels");
        else
            Information("MainClient finished joining Channels");

#if !DEBUG
        Information("Joining AnonClient channels");
        LoggerSetup.LogSwitch.MinimumLevel = LogEventLevel.Warning;
        bool success2 = await AnonClient.JoinChannels(channels.Where(x => x.Priority is < 50 and > -10).Select(x => x.Username));
        LoggerSetup.LogSwitch.MinimumLevel = ll;
        if (!success2)
            Warning("AnonClient failed to join some channels");
        else
            Information("AnonClient finished joining Channels");
#endif

        return StartupTaskState.Completed;
    }

    public static async Task JoinChannel(IvrUser user, int priority, bool isLogged)
    {
        await PostgresQueryLock.WaitAsync();
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
                Tags = null
            };

            _ = await Postgres.ExecuteAsync("insert into channels values (@DisplayName, @Username, @Id, @AvatarUrl, @Priority, @IsLogged, @DateJoined)", channelDto);
            _ = priority >= 50 ? await MainClient.JoinChannel(user.Login) : await AnonClient.JoinChannel(user.Login);

            Channels[channelDto.Username] = channelDto;
            ChannelsById[channelDto.Id] = channelDto;
        }
        finally
        {
            _ = PostgresQueryLock.Release();
        }
    }

    public static async Task PartChannel(long channelId)
    {
        await PostgresQueryLock.WaitAsync();
        try
        {
            _ = await Postgres.ExecuteAsync("delete from channels where id = @ChannelId", new { ChannelId = channelId });
            TwitchChannelDto channelDto = ChannelsById[channelId];
            if (channelDto.Priority >= 50)
                await MainClient.PartChannel(channelDto.Username);
            else
                await AnonClient.PartChannel(channelDto.Username);

            _ = Channels.Remove(channelDto.Username);
            _ = ChannelsById.Remove(channelDto.Id);
        }
        finally
        {
            _ = PostgresQueryLock.Release();
        }
    }
}