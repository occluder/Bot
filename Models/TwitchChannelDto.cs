namespace Bot.Models;

public class TwitchChannelDto
{
    public required string DisplayName { get; set; }
    public required string Username { get; set; }
    public required long Id { get; set; }
    public required string AvatarUrl { get; set; }
    public required int Priority { get; set; }
    public required bool IsLogged { get; set; }
    public required DateTime DateJoined { get; set; }
    public required string? Tags { get; set; }
    public bool PredictionsEnabled => this.Tags?.Contains("predictions") ?? false;
    public bool WatchFollows => this.Tags?.Contains("follows") ?? false;
}
