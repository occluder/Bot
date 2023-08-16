namespace Bot.Interfaces;

public interface IReloadable
{
    string ReloadKey { get; }
    ValueTask<bool> Reload();
}
