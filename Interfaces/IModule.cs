namespace Bot.Interfaces;

public interface IModule
{
    public bool Enabled { get; }
    public ValueTask Enable();
    public ValueTask Disable();
}
