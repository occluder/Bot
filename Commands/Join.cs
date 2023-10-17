using System.Net;
using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

internal class Join: ChatCommand
{
    public override CommandInfo Info { get; } = new(
        "join",
        "Join a channel",
        TimeSpan.Zero,
        CommandPermission.Whitelisted
    );

    public Join()
    {
        AddArgument(new CommandArgument("Login", 1, typeof(string)));
        AddArgument(new CommandArgument("Priority", 2, typeof(int), true));
        AddArgument(new CommandArgument("IsLogged", 3, typeof(bool), true));
    }

    public override async ValueTask Run(Privmsg message)
    {
        string login = GetArgument<string>("Login");
        _ = TryGetArgument("Priority", out int? priority);
        _ = TryGetArgument("IsLogged", out bool? logged);
        OneOf<IvrUser[], HttpStatusCode, Exception> response =
            await GetFromRequest<IvrUser[]>($"https://api.ivr.fi/v2/twitch/user?login={login}");
        if (response.TryPickT0(out IvrUser[] users, out _))
        {
            await JoinChannel(users[0], priority ?? 0, logged ?? true);
            await message.ReplyWith("👍");
        }
        else
        {
            await message.ReplyWith("Failed");
        }
    }
}