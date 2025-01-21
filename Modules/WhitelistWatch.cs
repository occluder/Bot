using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;
public class WhitelistWatch: BotModule
{
    private async ValueTask OnMessage(Privmsg message)
    {
        if (!UserPermissions[message.Author.Id].IsWhitelisted)
        {
            return;
        }

        var obj = new WhitelistedMessage
        (
            Guid.Parse(message.Id),
            message.Author.Name,
            message.Author.Id,
            message.Channel.Name,
            message.Channel.Id,
            message.Content,
            message.Reply.HasContent,
            message.TmiSentTs
        );

        using var conn = await NewDbConnection();
        try
        {
            await RedisPubSub.PublishAsync("bot:whitelisted_message", obj);
            await conn.ExecuteAsync(
                """
                insert into
                    whitelisted_message
                values (
                    @Id,
                    @Username,
                    @UserId,
                    @Channel,
                    @ChannelId,
                    @Message,
                    @IsReply,
                    @TimeSent
                )
                """, obj, commandTimeout: 10
            );
        }
        catch (Exception ex)
        {
            Error(ex, "Error inserting whitelisted message");
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

    private record struct WhitelistedMessage(
        Guid Id,
        string Username,
        long UserId,
        string Channel,
        long ChannelId,
        string Message,
        bool IsReply,
        long TimeSent
    );
}
