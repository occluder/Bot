using System.Data;
using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class WhoIs: ChatCommand
{
    public WhoIs()
    {
        AddArgument(new CommandArgument("Target", 1, typeof(string)));
    }

    public override CommandInfo Info { get; } = new(
        "whois",
        "Look up info about a user",
        TimeSpan.FromSeconds(5),
        CommandPermission.Moderators
    );

    public override async ValueTask Run(Privmsg message)
    {
        using var conn = await NewDbConnection();
        UserDto[] queryResult = [];
        if (long.TryParse(GetArgument<string>("Target"), out long userId))
        {
            queryResult = await IdLookup(userId, conn);
        }

        if (queryResult.Length == 0)
        {
            queryResult = await NameLookup(GetArgument<string>("Target"), conn);
        }

        if (queryResult.Length == 0)
        {
            Debug("No results found for user lookup: {Target}", GetArgument<string>("Target"));
            await message.ReplyWith("User not found :(");
            return;
        }

        await message.ReplyWith(
            $"{queryResult.MaxBy(x => x.AddedAt)!.Username} ({queryResult[0].UserId}) aliases: " +
            $"{string.Join(", ", queryResult.Select(x => $"{x.Username} ({DateTimeOffset.FromUnixTimeMilliseconds(x.AddedAt):Y})"))}"
        );
    }

    static async Task<UserDto[]> IdLookup(long userId, IDbConnection connection)
    {
        try
        {

            return [.. await connection.QueryAsync<UserDto>(
                """
                    select username, user_id
                    from users
                    where user_id = @UserId
                """,
                new
                {
                    UserId = userId
                }
            )];
        }
        catch (Exception ex)
        {
            Error(ex, "An error occurred while fetching the user's aliases");
            return [];
        }
    }

    static async Task<UserDto[]> NameLookup(string username, IDbConnection connection)
    {
        try
        {

            return [.. await connection.QueryAsync<UserDto>(
                """
                    select username, user_id
                    from users
                    where user_id = (select max(user_id) from users where username = @Username)
                """,
                new
                {
                    Username = username.ToLower()
                }
            )];
        }
        catch (Exception ex)
        {
            Error(ex, "An error occurred while fetching the user's aliases");
            return [];
        }
    }
}