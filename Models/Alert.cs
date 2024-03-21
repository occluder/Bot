namespace Bot.Models;

public record Alert(string Id, bool Active, Mission Mission);
public record Mission(string NodeKey, int MinEnemyLevel, int MaxEnemyLevel, Reward Reward);
public record Reward(string AsString, string ItemString);
