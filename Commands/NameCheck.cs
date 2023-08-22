using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class NameCheck : ChatCommand
{
    public override CommandInfo Info => new("namecheck", "Check names of a user's ID", TimeSpan.FromSeconds(5), CommandPermission.Moderators);

    public NameCheck()
    {
        AddArgument(new("UserId", 1, typeof(long)));
    }

    public override async ValueTask Run(Privmsg message)
    {
        ValueTask check = CheckArguments(message);
        if (!check.IsCompleted)
        {
            await check;
            return;
        }

        var queryResult = await Postgres.QueryAsync<UserDto>("SELECT username, user_id FROM collected_users WHERE user_id = @UserId", new
        {
            UserId = GetArgument<long>("UserId")
        });

        string[] aliases = queryResult.Select(x => x.Username).ToArray();
        if (aliases.Length == 0)
        {
            await message.ReplyWith("User not found :(");
            return;
        }

        await message.ReplyWith($"🔎 {GetArgument<long>("UserId")}: {string.Join(", ", aliases)}");
    }
}
