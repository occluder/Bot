using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

internal class Join : IChatCommand
{
    public CommandInfo Info => new("join", "Join a channel", TimeSpan.Zero, CommandPermission.Whitelisted);

    public async ValueTask Run(Privmsg message)
    {
        string[] args = message.Content.Split(' ');
        switch (args.Length)
        {
            case 2:
                var response = await GetFromRequest<IvrUser[]>($"https://api.ivr.fi/v2/twitch/user?login={args[1]}");
                if (response.TryPickT0(out IvrUser[] users, out _))
                {
                    await JoinChannel(users[0], 0, true);
                    await message.ReplyWith("👍");
                }
                else
                    await message.ReplyWith("Failed");

                return;
            
            case 3:
                var response2 = await GetFromRequest<IvrUser[]>($"https://api.ivr.fi/v2/twitch/user?login={args[1]}");
                if (response2.TryPickT0(out IvrUser[] users2, out _))
                {
                    await JoinChannel(users2[0], int.TryParse(args[2], out var i) ? i : 0, true);
                    await message.ReplyWith("👍");
                }
                else
                    await message.ReplyWith("Failed");

            break;

            case 4:
                var response3 = await GetFromRequest<IvrUser[]>($"https://api.ivr.fi/v2/twitch/user?login={args[1]}");
                if (response3.TryPickT0(out IvrUser[] users3, out _))
                {
                    await JoinChannel(users3[0], int.TryParse(args[2], out var i) ? i : 0, !bool.TryParse(args[3], out var b) || b);
                    await message.ReplyWith("👍");
                }
                else
                    await message.ReplyWith("Failed");

                break;

            default: return;
        }
    }
}
