﻿namespace Bot.Models;

public abstract class BotModule
{
    public bool Enabled { get; private set; }
    public string Name => GetType().Name;

    public async ValueTask Enable()
    {
        if (this.Enabled)
            return;

        await OnModuleEnabled();
        this.Enabled = true;
        await Settings.EnableModule(this.Name);
    }
    public async ValueTask Disable()
    {
        if (!this.Enabled)
            return;

        await OnModuleDisabled();
        this.Enabled = false;
        await Settings.DisableModule(this.Name);
    }

    protected virtual ValueTask OnModuleEnabled() => default;
    protected virtual ValueTask OnModuleDisabled() => default;
}
