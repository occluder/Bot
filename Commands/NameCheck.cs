using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class NameCheck : IChatCommand
{
    public CommandInfo Info => new("namecheck", "Check names of a user's ID", TimeSpan.FromSeconds(5), CommandPermission.Moderators);

    public async ValueTask Run(Privmsg message)
    {
        string[] args = message.Content.Split(' ');

        if (args.Length < 2)
        {
            await message.ReplyWith("Argument 2 missing: user ID");
            return;
        }

        if (!long.TryParse(args[1], out var id))
        {
            await message.ReplyWith("Argument 2 must be int");
            return;
        }

        var queryResult = await Postgres.QueryAsync<UserDto>("SELECT username, user_id FROM collected_users WHERE user_id = @UserId", new
        {
            UserId = id
        });

        string[] aliases = queryResult.Select(x => x.Username).ToArray();
        if (aliases.Length == 0)
        {
            await message.ReplyWith("User not found :(");
            return;
        }

        await message.ReplyWith($"🔎 {id}: {string.Join(", ", aliases)}");
    }
}
