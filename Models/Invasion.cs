namespace Bot.Models;

public record Invasion(string Id, string NodeKey, InvasionParty Attacker, InvasionParty Defender, bool VsInfestation, bool Completed);
public record InvasionParty(Reward Reward);
