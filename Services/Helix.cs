using MiniTwitch.Helix;

namespace Bot.Services;

public static class Helix
{
    public static HelixWrapper HelixClient { get; set; } = default!;
}