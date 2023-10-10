using MiniTwitch.Helix;

namespace Bot.Services;

public static class HelixApi
{
    public static HelixClient Client { get; set; } = default!;
}