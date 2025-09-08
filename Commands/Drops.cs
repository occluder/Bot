using Bot.Enums;
using Bot.Models;
using Bot.Services;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class Drops: ChatCommand
{
    public Drops()
    {
        AddArgument(new("Item Name", typeof(string), TakeRemaining: true));
    }

    public override CommandInfo Info { get; } = new(
        "drops",
        "Get the drop locations of an item",
        TimeSpan.FromSeconds(5),
        CommandPermission.Everyone
    );

    public override async ValueTask Run(Privmsg message)
    {
        string item = GetArgument("Item Name").AssumedString;
        var response = await GetFromRequest<ItemDrop[]>($"https://api.warframestat.us/drops/search/{item}");
        if (!response.TryPickT0(out ItemDrop[]? dropInfo, out var error))
        {
            Warning($"Error from https://api.warframestat.us/drops/search/{item}", item);
            await error.Match(
                statusCode => message.ReplyWith($"Received bad status code {statusCode} :("),
                exception => message.ReplyWith($"Error handling code: ({exception.GetType().Name}) {exception.Message}")
            );

            return;
        }

        switch (dropInfo.Length)
        {
            case 0:
                await message.ReplyWith("No drop locations for that item were found");
                break;

            case < 4:
                string m = string.Join(", ", dropInfo
                    .OrderByDescending(d => d.chance)
                    .Select(d => $"{d.place} ({d.chance:0.##}%)")
                );

                await message.ReplyWith(m);
                break;

            default:
                ItemDrop? drop = dropInfo.MaxBy(d => d.chance);
                string fullString = string.Join("\r\n", dropInfo
                    .Skip(1)
                    .OrderByDescending(d => d.chance)
                    .Select(d => $"{d.place} ({d.chance:0.##}%)")
                );

                OneOf<string, Exception> hasteResponse = await TextUploadService.UploadToHaste(fullString);
                hasteResponse.TryPickT0(out string? link, out _);
                await message.ReplyWith(
                    $"{drop!.place} ({drop.chance:0.##}%) " +
                    $"and {dropInfo.Length - 1} other sources: {link ?? "[upload failed]"}"
                );

                break;
        }
    }
}