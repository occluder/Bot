using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

public class ModeratorTracker: BotModule
{
    long _commit_time;
    List<object> _mods = [];

    async ValueTask OnMessage(Privmsg message)
    {
        if (!message.Author.IsMod)
        {
            return;
        }

        _mods.Add(new
        {
            Username = message.Author.Name,
            UserId = message.Author.Id,
            Channel = message.Channel.Name,
            ChannelId = message.Channel.Id,
            LastSeen = message.SentTimestamp.ToUnixTimeSeconds(),
        });

        if (Unix() - _commit_time < 60)
        {
            return;
        }

        await Commit();
        _commit_time = Unix();
    }

    async Task Commit()
    {
        object[] vals = [.. _mods];
        _mods.Clear();
        await LiveConnectionLock.WaitAsync();
        try
        {
            await LiveDbConnection.ExecuteAsync(
                """
                insert into channel_moderator (
                    username,
                    user_id,
                    channel,
                    channel_id,
                    last_seen
                ) values (
                    @Username,
                    @UserId,
                    @Channel,
                    @ChannelId,
                    @LastSeen
                ) on conflict(user_id, channel_id) do update set
                    username = excluded.username,
                    last_seen = excluded.last_seen
                """, vals, commandTimeout: 10
            );
        }
        catch (Exception ex)
        {
            Error(ex, "Failed to insert mods");
        }
        finally
        {
            LiveConnectionLock.Release();
        }
    }


    protected override ValueTask OnModuleEnabled()
    {
        MainClient.OnMessage += OnMessage;
        AnonClient.OnMessage += OnMessage;
        return default;
    }

    protected override ValueTask OnModuleDisabled()
    {
        MainClient.OnMessage -= OnMessage;
        AnonClient.OnMessage -= OnMessage;
        return default;
    }
}
