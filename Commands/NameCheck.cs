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
        IEnumerable<UserDto> queryResult = await LiveDbConnection.QueryAsync<UserDto>(
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

        string[] aliases = queryResult.Select(x => x.Username).ToArray();
        if (aliases.Length == 0)
        {
            await message.ReplyWith("User not found :(");
            return;
        }

        await message.ReplyWith($"{aliases.Length} aliases: {string.Join(", ", aliases)}");
    }
}