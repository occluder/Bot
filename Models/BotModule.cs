using Bot.Interfaces;

namespace Bot.Models;

public abstract class BotModule
{
    public bool Enabled { get; private set; }
    private string Name => GetType().Name;

    public async ValueTask Enable()
    {
        if (this.Enabled)
            return;

        await OnModuleEnabled();
        this.Enabled = true;
        await Settings.EnableModule(Name);
    }
    public async ValueTask Disable()
    {
        if (!this.Enabled)
            return;

        await OnModuleDisabled();
        this.Enabled = false;
        await Settings.EnableModule(Name);
    }

    protected virtual ValueTask OnModuleEnabled() => default;
    protected virtual ValueTask OnModuleDisabled() => default;
}
