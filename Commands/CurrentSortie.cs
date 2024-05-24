using System.Net;
using Bot.Enums;
using Bot.Models;
using MiniTwitch.Irc.Models;

namespace Bot.Commands;

public class CurrentSortie: ChatCommand
{
    public override CommandInfo Info { get; } = new(
        "sortie",
        "Fetches the current sortie",
        TimeSpan.FromSeconds(3),
        CommandPermission.Everyone
    );

    public override async ValueTask Run(Privmsg message)
    {
        (bool exists, Sortie sortie) = await Cache.TryGetObjectAsync<Sortie>("warframe:data:sortie");
        if (!exists)
        {
            Debug("Sortie was not cached");
            OneOf<Sortie, HttpStatusCode, Exception> request =
                await GetFromRequest<Sortie>("https://api.warframestat.us/pc/sortie?language=en");

            await request.Match(
                s => SetSortieAndRerun(message, s),
                statusCode => message.ReplyWith($"Received bad status code {statusCode} :("),
                exception => message.ReplyWith($"Error handling code: ({exception.GetType().Name}) {exception.Message}")
            );

            return;
        }

        Debug("Sortie is cached");
        string sortieString = $"[{sortie.Faction}] " +
                              $"\ud83d\udd34 {VariantString(sortie.Variants[0], sortie)} " +
                              $"\ud83d\udfe2 {VariantString(sortie.Variants[1], sortie)} " +
                              $"\ud83d\udd35 {VariantString(sortie.Variants[2], sortie)} " +
                              $"-- Ends in {PrettyTimeString(sortie.Expiry.ToLocalTime() - DateTime.Now)}";

        await message.ReplyWith(sortieString);
    }

    private string VariantString(Variant variant, Sortie sortie) =>
        variant.MissionType == "Assassination"
            ? $"{sortie.Boss} Assassination ({ModifierOf(variant)})"
            : $"{variant.MissionType} ({ModifierOf(variant)})";

    private async ValueTask SetSortieAndRerun(Privmsg message, Sortie sortie)
    {
        Debug("Caching sortie");
        await Cache.SetObjectAsync("warframe:data:sortie", sortie, sortie.Expiry.ToLocalTime() - DateTime.Now);
        await Run(message);
    }

    private string ModifierOf(Variant variant)
    {
        string[] split = variant.Modifier.Split(": ");
        return split[0] switch
        {
            "Eximus Stronghold" => "+Eximus",
            "Weapon Restriction" => split[1],
            "Augmented Enemy Armor" => "+Armor",
            "Enhanced Enemy Shields" => "+Shields",
            "Enemy Elemental Enhancement" => split[1] switch
            {
                "Heat" => "+🔥",
                "Cold" => "+❄",
                "Electricity" => "+⚡",
                "Magnetic" => "+🧲",
                "Blast" => "+💥",
                "Radiation" => "+☢",
                "Viral" => "+🦠",
                "Corrosive" => "+🧪",
                "Toxin" => "+\u2620\ufe0f",
                // Gas
                _ => '+' + split[1]
            },
            "Enemy Physical Enhancement" => split[1] switch
            {
                "Puncture" => "+📌",
                "Slash" => "+🔪",
                "Impact" => "+🔨",
                _ => split[1]
            },
            _ => variant.Modifier
        };
    }
}