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
        AddArgument(new("UserId", typeof(long)));
        AddArgument(new("Action", typeof(string)));
        AddArgument(new("BlockDurationHours", typeof(int), true));
    }

    public override ValueTask Run(Privmsg message)
    {
        return GetArgument("Action").AssumedString switch
        {
            "blacklist" => BlackList(message),
            "unblacklist" => UnBlackList(message),
            _ => message.ReplyWith("Action unknown")
        };
    }

    private async ValueTask BlackList(Privmsg msg)
    {
        long uid = GetArgument("UserId").AssumedLong;
        var response = await GetFromRequest<IvrUser[]>($"https://api.ivr.fi/v2/twitch/user?id={uid}");
        if (response.TryPickT0(out IvrUser[] users, out _))
        {
            using var conn = await NewDbConnection();
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

                await conn.ExecuteAsync(
                    "insert into user_permissions values (@Username, @UserId, @Permissions, @LastModified)",
                    permission
                );

                UserPermissions.Add(long.Parse(user.Id), permission);
                await msg.ReplyWith($"Blacklisted user: {user.Login} id:{user.Id}");
            }
            catch (Exception ex)
            {
                await msg.ReplyWith("Failed to blacklist user");
                Error(ex, "Failed to blacklist user");
            }
        }
        else
        {
            await msg.ReplyWith("Request error");
        }
    }

    private async ValueTask UnBlackList(Privmsg msg)
    {
        long uid = GetArgument("UserId").AssumedLong;
        using var conn = await NewDbConnection();
        try
        {
            int count = await conn.ExecuteAsync("delete from user_permissions where user_id = @UserId", new
            {
                UserId = uid
            });

            UserPermissions.Remove(uid);
            await msg.ReplyWith(count == 1 ? "Successfully unblacklisted" : "Failed to unblacklist");
        }
        catch (Exception ex)
        {
            await msg.ReplyWith("Failed to unblacklist user");
            Error(ex, "Failed to unblacklist user");
        }
    }
}