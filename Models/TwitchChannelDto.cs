namespace Bot.Models;

public class TwitchChannelDto
{
    private bool? _follows;

    private bool? _logged;
    private bool? _predictions;
    private bool? _susCheck;
    private bool? _noRelay;
    public required string DisplayName { get; set; }
    public required string ChannelName { get; set; }
    public required long ChannelId { get; set; }
    public required string AvatarUrl { get; set; }
    public required int Priority { get; set; }
    public required string? Tags { get; set; }
    public required long DateAdded { get; init; }
    public bool IsLogged => _logged ??= this.Tags is null || !this.Tags.Contains("nologs");
    public bool PredictionsEnabled => _predictions ??= this.Tags is not null && this.Tags.Contains("predictions");
    public bool WatchFollows => _follows ??= this.Tags is not null && this.Tags.Contains("follows");
    public bool SusCheck => _susCheck ??= this.Tags is not null && this.Tags.Contains("suscheck");
    public bool NoRelay => _noRelay ??= this.Tags is not null && this.Tags.Contains("norelay");
}