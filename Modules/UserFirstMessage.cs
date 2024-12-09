using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;
internal class UserFirstMessage: BotModule
{
    static async ValueTask OnMessage(Privmsg msg)
    {
        if (!msg.IsFirstMessage)
        {
            return;
        }

        using var db = await NewDbConnection();
        try
        {
            await db.ExecuteAsync(
                """
                    INSERT INTO 
                        user_first_message 
                    VALUES 
                        (@Username, @UserId, @Channel, @ChannelId, @Message, @TimeSent)
                """,
                new
                {
                    Username = msg.Author.Name,
                    UserId = msg.Author.Id,
                    Channel = msg.Channel.Name,
                    ChannelId = msg.Channel.Id,
                    Message = msg.Content,
                    TimeSent = msg.TmiSentTs,
                }
            );
        }
        catch (Exception e)
        {
            ForContext<UserFirstMessage>().ForContext("MsgInfo", msg, true).Error(e, "Execute operation failed");
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
