using Bot.Enums;

namespace Bot.Models;
public record CommandInfo(string Name, string Description, TimeSpan Cooldown, CommandPermission Permission);
