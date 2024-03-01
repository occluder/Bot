using System.Net;
using Bot.Enums;
using Bot.Models;
using Bot.Services;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class Drops: ChatCommand
{
    public Drops()
    {
        AddArgument(new("ItemName", 1, typeof(string)));
    }

    public override CommandInfo Info { get; } = new(
        "drops",
        "Get the drop locations of an item",
        TimeSpan.FromSeconds(5),
        CommandPermission.Everyone
    );

    public override async ValueTask Run(Privmsg message)
    {
        string[] split = message.Content.Split(' ');
        string item = string.Join(' ', split[1..]);
        OneOf<ItemDrop[], HttpStatusCode, Exception> response =
            await GetFromRequest<ItemDrop[]>($"https://api.warframestat.us/drops/search/{item}");

        if (!response.TryPickT0(out ItemDrop[]? dropInfo, out OneOf<HttpStatusCode, Exception> error))
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
                    .Select(d => $"{d.place} ({d.chance:00.00}%)")
                );

                await message.ReplyWith(m);
                break;

            default:
                ItemDrop? drop = dropInfo.MaxBy(d => d.chance);
                string fullString = string.Join("\r\n", dropInfo
                    .Skip(1)
                    .OrderByDescending(d => d.chance)
                    .Select(d => $"{d.place} ({d.chance:00.00}%)")
                );
                
                OneOf<string, Exception> hasteResponse = await TextUploadService.UploadToHaste(fullString);
                hasteResponse.TryPickT0(out string? link, out _);
                await message.ReplyWith($"{drop!.place} ({drop.chance:00.00}%) " +
                                        $"and {dropInfo.Length - 1} other sources: {link ?? "[upload failed]"}");
                break;
        }
    }
}