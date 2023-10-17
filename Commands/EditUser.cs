using System.Net;
using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class EditUser: ChatCommand
{
    public override CommandInfo Info { get; } = new(
        "edituser",
        "Edit a user's permissions",
        TimeSpan.Zero,
        CommandPermission.Whitelisted
    );

    public EditUser()
    {
        AddArgument(new("UserId", 1, typeof(long)));
        AddArgument(new("Action", 2, typeof(string)));
        AddArgument(new("BlockDurationHours", 3, typeof(int), true));
    }

    public override ValueTask Run(Privmsg message)
    {
        return GetArgument<string>("Action") switch
        {
            "blacklist" => BlackList(message),
            "unblacklist" => UnBlackList(message),
            _ => message.ReplyWith("Action unknown")
        };
    }

    private async ValueTask BlackList(Privmsg msg)
    {
        long uid = GetArgument<long>("UserId");
        OneOf<IvrUser[], HttpStatusCode, Exception> response =
            await GetFromRequest<IvrUser[]>($"https://api.ivr.fi/v2/twitch/user?id={uid}");

        if (response.TryPickT0(out IvrUser[] users, out _))
        {
            await PostgresQueryLock.WaitAsync();
            try
            {
                IvrUser user = users.Single();
                await Postgres.ExecuteAsync("insert into blacklisted_users values (@Username, @UserId)", new
                {
                    Username = user.Login,
                    UserId = long.Parse(user.Id)
                });

                BlackListedUserIds.Add(long.Parse(user.Id));
                await msg.ReplyWith($"Blacklisted user: {user.Login} id:{user.Id}");
            }
            finally
            {
                PostgresQueryLock.Release();
            }
        }
        else
        {
            await msg.ReplyWith("Request error");
        }
    }

    private async ValueTask UnBlackList(Privmsg msg)
    {
        long uid = GetArgument<long>("UserId");
        await PostgresQueryLock.WaitAsync();
        try
        {
            int count = await Postgres.ExecuteAsync("delete from blacklisted_users where id = @UserId", new
            {
                UserId = uid
            });

            BlackListedUserIds.Remove(uid);
            await msg.ReplyWith(count == 1 ? "Successfully unblacklisted" : "Failed to unblacklist");
        }
        finally
        {
            PostgresQueryLock.Release();
        }
    }
}