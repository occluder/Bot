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

        using var conn = await NewDbConnection();
        try
        {
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
                """,
                new
                {
                    Id = Guid.Parse(message.Id),
                    Username = message.Author.Name,
                    UserId = message.Author.Id,
                    Channel = message.Channel.Name,
                    ChannelId = message.Channel.Id,
                    Message = message.Content,
                    IsReply = message.Reply.HasContent,
                    TimeSent = message.TmiSentTs,
                }, commandTimeout: 10
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
}
