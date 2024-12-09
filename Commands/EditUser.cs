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
            await LiveConnectionLock.WaitAsync();
            try
            {
                IvrUser user = users.Single();
                UserPermissionDto permission = new()
                {
                    Username = user.Login,
                    UserId = long.Parse(user.Id),
                    Permissions = "blacklisted",
                    LastModified = msg.SentTimestamp.ToUnixTimeSeconds()
                };

                await LiveDbConnection.ExecuteAsync(
                    "insert into user_permissions values (@Username, @UserId, @Permissions, @LastModified)",
                    permission
                );

                UserPermissions.Add(long.Parse(user.Id), permission);
                await msg.ReplyWith($"Blacklisted user: {user.Login} id:{user.Id}");
            }
            finally
            {
                LiveConnectionLock.Release();
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
        await LiveConnectionLock.WaitAsync();
        try
        {
            int count = await LiveDbConnection.ExecuteAsync("delete from user_permissions where user_id = @UserId", new
            {
                UserId = uid
            });

            UserPermissions.Remove(uid);
            await msg.ReplyWith(count == 1 ? "Successfully unblacklisted" : "Failed to unblacklist");
        }
        finally
        {
            LiveConnectionLock.Release();
        }
    }
}