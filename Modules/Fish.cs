using Bot.Interfaces;
using Bot.Utils;

namespace Bot.Modules;

internal class Fish : IModule
{
    public bool Enabled { get; private set; }
    private readonly BackgroundTimer _timer;

    public Fish()
    {
        _timer = new(TimeSpan.FromHours(1), TryFish);
    }

    private Task TryFish()
    {
        if (!this.Enabled)
            return Task.CompletedTask;

        if (Random.Shared.Next(10) != 0)
            return Task.CompletedTask;

        return MainClient.SendMessage("pajlada", "$fish").AsTask();
    }

    public async ValueTask Enable()
    {
        if (this.Enabled)
            return;

        _timer.Start();
        this.Enabled = true;
        await Settings.EnableModule(nameof(Fish));
    }

    public async ValueTask Disable()
    {
        if (!this.Enabled)
            return;

        await _timer.StopAsync();
        this.Enabled = false;
        await Settings.DisableModule(nameof(Fish));
    }
}
