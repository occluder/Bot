using Bot.Models;
using Bot.Utils;

namespace Bot.Modules;

internal class Fish: BotModule
{
    private readonly BackgroundTimer _timer;

    public Fish()
    {
        _timer = new(TimeSpan.FromHours(1), TryFish);
    }

    private Task TryFish()
    {
        if (!this.Enabled)
            return Task.CompletedTask;

        if (Random.Shared.Next(20) != 0)
            return Task.CompletedTask;

        return MainClient.SendMessage("pajlada", "$fish").AsTask();
    }

    protected override ValueTask OnModuleEnabled()
    {
        _timer.Start();
        return default;
    }
    protected override async ValueTask OnModuleDisabled() => await _timer.StopAsync();
}
