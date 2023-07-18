using Bot.Enums;

namespace Bot.Models;
internal record CommandInfo(string Name, string Description, TimeSpan Cooldown, CommandPermission Permission);
