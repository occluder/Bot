namespace Bot.Models;

internal class StreamInfo
{
    public string Title { get; set; } = default!;
    public string Game { get; set; } = default!;
    public bool IsLive { get; set; }
    public DateTime StartedAt { get; set; }
}
