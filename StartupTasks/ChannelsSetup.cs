using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;
using Serilog.Events;

namespace Bot.StartupTasks;

internal class ChannelsSetup: IStartupTask
{
    public static Dictionary<string, TwitchChannelDto> Channels { get; } = [];
    public static Dictionary<long, TwitchChannelDto> ChannelsById { get; } = [];

    public async ValueTask<StartupTaskState> Run()
    {
        TwitchChannelDto[] channels;
        using var conn = await NewDbConnection();
        try
        {
            channels = [.. await conn.QueryAsync<TwitchChannelDto>("select * from channels where priority > -10", commandTimeout: 5)];
        }
        catch (Exception ex)
        {
            ForContext<ChannelsSetup>().Fatal(ex, "{ClassName} Failed to setup channels");
            return StartupTaskState.Failed;
        }

        foreach (TwitchChannelDto channel in channels)
        {
            if (!Channels.TryAdd(channel.ChannelName, channel) && Channels[channel.ChannelName].DateAdded < channel.DateAdded)
            {
                ForContext<ChannelsSetup>().Warning("Channel {ChannelName} already exists, but is older than the new one. Updating it.", channel.ChannelName);
                Channels[channel.ChannelName] = channel;
            }
            if (!ChannelsById.TryAdd(channel.ChannelId, channel) && ChannelsById[channel.ChannelId].DateAdded < channel.DateAdded)
            {
                ForContext<ChannelsSetup>().Warning("Channel {ChannelName} already exists, but is older than the new one. Updating it.", channel.ChannelName);
                ChannelsById[channel.ChannelId] = channel;
            }
        }

        Information("{ChannelCount} channels loaded. {PrioritizedCount} prioritized",
            channels.Length, channels.Count(x => x.Priority >= 50));

        Information("{ChannelCount} channels are logged", channels.Count(x => x.IsLogged));
        Information("Joining MainClient channels");
        LogEventLevel ll = LoggerSetup.LogSwitch.MinimumLevel;
        LoggerSetup.LogSwitch.MinimumLevel = LogEventLevel.Warning;
        bool success = await MainClient.JoinChannels(channels.Where(x => x.Priority >= 50).Select(x => x.ChannelName));
        LoggerSetup.LogSwitch.MinimumLevel = ll;
        if (!success)
        {
            Warning("MainClient failed to join some channels");
        }
        else
        {
            Information("MainClient finished joining Channels");
        }

#if !DEBUG
        Information("Joining AnonClient channels");
        LoggerSetup.LogSwitch.MinimumLevel = LogEventLevel.Warning;
        bool success2 =
            await AnonClient.JoinChannels(channels.Where(x => x.Priority is < 50 and > -10).Select(x => x.ChannelName));
        LoggerSetup.LogSwitch.MinimumLevel = ll;
        if (!success2)
        {
            Warning("AnonClient failed to join some channels");
        }
        else
        {
            Information("AnonClient finished joining Channels");
        }
#endif

        return StartupTaskState.Completed;
    }

    public static async Task JoinChannel(IvrUser user, int priority, bool isLogged)
    {
        using var conn = await NewDbConnection();
        try
        {
            TwitchChannelDto channelDto = new()
            {
                DisplayName = user.DisplayName,
                ChannelName = user.Login,
                ChannelId = long.Parse(user.Id),
                AvatarUrl = user.Logo,
                Priority = priority,
                DateAdded = UnixMs(),
                Tags = isLogged ? null : "nologs"
            };

            _ = await conn.ExecuteAsync(
                "insert into channels values (@DisplayName, @ChannelName, @ChannelId, @AvatarUrl, @Priority, @Tags, @DateAdded)",
                channelDto
            );

            _ = priority >= 50 ? await MainClient.JoinChannel(user.Login) : await AnonClient.JoinChannel(user.Login);

            Channels[channelDto.ChannelName] = channelDto;
            ChannelsById[channelDto.ChannelId] = channelDto;
        }
        catch (Exception ex)
        {
            ForContext<ChannelsSetup>().Error(ex, "Failed to join channel {ChannelName}", user.Login);
        }
    }

    public static async Task PartChannel(long channelId)
    {
        using var conn = await NewDbConnection();
        try
        {
            _ = await conn.ExecuteAsync("""
                UPDATE channels 
                SET
                    priority = -100
                WHERE
                    channel_id = @ChannelId
                """,
                new { ChannelId = channelId }
            );

            TwitchChannelDto channelDto = ChannelsById[channelId];
            if (channelDto.Priority >= 50)
                await MainClient.PartChannel(channelDto.ChannelName);
            else
                await AnonClient.PartChannel(channelDto.ChannelName);

            _ = Channels.Remove(channelDto.ChannelName);
            _ = ChannelsById.Remove(channelDto.ChannelId);
        }
        catch (Exception ex)
        {
            ForContext<ChannelsSetup>().Error(ex, "Failed to part channel {ChannelId}", channelId);
        }
    }
}