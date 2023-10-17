using MiniTwitch.Helix;

namespace Bot.Services;

public static class Helix
{
    public static HelixClient HelixClient { get; set; } = default!;
}