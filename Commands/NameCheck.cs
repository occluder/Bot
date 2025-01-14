using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class NameCheck: ChatCommand
{
    public NameCheck()
    {
        AddArgument(new CommandArgument("Username", 1, typeof(string)));
    }

    public override CommandInfo Info { get; } = new(
        "namecheck",
        "Check aliases of a user",
        TimeSpan.FromSeconds(5),
        CommandPermission.Moderators
    );

    public override async ValueTask Run(Privmsg message)
    {
        using var conn = await NewDbConnection();
        IEnumerable<UserDto> queryResult = default!;
        try
        {
            queryResult = await conn.QueryAsync<UserDto>(
                """
                    select username, user_id
                    from users
                    where user_id = (select max(user_id) from users where username = @Username)
                """,
                new
                {
                    Username = GetArgument<string>("Username")
                }
            );
        }
        catch (Exception ex)
        {
            await message.ReplyWith("An error occurred while fetching the user's aliases");
            Error(ex, "An error occurred while fetching the user's aliases");
            return;
        }

        string[] aliases = queryResult.Select(x => x.Username).ToArray();
        if (aliases.Length == 0)
        {
            await message.ReplyWith("User not found :(");
            return;
        }

        await message.ReplyWith($"{aliases.Length} aliases: {string.Join(", ", aliases)}");
    }
}