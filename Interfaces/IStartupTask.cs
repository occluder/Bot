using Bot.Enums;

namespace Bot.Interfaces;

public interface IStartupTask
{
    public ValueTask<StartupTaskState> Run();
}