using MiniTwitch.PubSub;

namespace Bot.Services;

public static class PubSub
{
    public static PubSubClient TwitchPubSub { get; set; } = default!;
}