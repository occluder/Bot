using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class NameCheck: ChatCommand
{
    public NameCheck()
    {
        AddArgument(new CommandArgument("UserId", 1, typeof(long)));
    }

    public override CommandInfo Info { get; } = new(
        "namecheck",
        "Check names of a user's ID",
        TimeSpan.FromSeconds(5),
        CommandPermission.Moderators
    );

    public override async ValueTask Run(Privmsg message)
    {
        IEnumerable<UserDto> queryResult = await Postgres.QueryAsync<UserDto>(
            "SELECT username, user_id FROM users WHERE user_id = @UserId",
            new
            {
                UserId = GetArgument<long>("UserId")
            }
        );

        string[] aliases = queryResult.Select(x => x.Username).ToArray();
        if (aliases.Length == 0)
        {
            await message.ReplyWith("User not found :(");
            return;
        }

        await message.ReplyWith($"🔎 {GetArgument<long>("UserId")}: {string.Join(", ", aliases)}");
    }
}