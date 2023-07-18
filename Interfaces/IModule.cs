namespace Bot.Interfaces;

internal interface IModule
{
    public bool Enabled { get; }
    public ValueTask Enable();
    public ValueTask Disable();
}
