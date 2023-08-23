using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

internal class Join : ChatCommand
{
    public override CommandInfo Info => new("join", "Join a channel", TimeSpan.Zero, CommandPermission.Whitelisted);

    public Join()
    {
        AddArgument(new("Login", 1, typeof(string)));
        AddArgument(new("Priority", 2, typeof(int), Optional: true));
        AddArgument(new("IsLogged", 3, typeof(bool), Optional: true));
    }

    public override async ValueTask Run(Privmsg message)
    {
        ValueTask check = CheckArguments(message);
        if (!check.IsCompleted)
        {
            await check;
            return;
        }

        string login = GetArgument<string>("Login");
        _ = TryGetArgument<int?>("Priority", out int? priority);
        _ = TryGetArgument<bool?>("IsLogged", out bool? logged);
        OneOf<IvrUser[], System.Net.HttpStatusCode, Exception> response = await GetFromRequest<IvrUser[]>($"https://api.ivr.fi/v2/twitch/user?login={login}");
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
