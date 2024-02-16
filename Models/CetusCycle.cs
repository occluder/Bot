namespace Bot.Models;

public record CetusCycle(
    string id,
    DateTimeOffset expiry,
    DateTimeOffset activation,
    bool isDay,
    string state,
    string timeLeft,
    bool isCetus,
    string shortString
);