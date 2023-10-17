using Bot.Enums;
using Bot.Handlers;
using Bot.Interfaces;

namespace Bot.StartupTasks;

internal class InitHandlers: IStartupTask
{
    public ValueTask<StartupTaskState> Run()
    {
        ChatHandler.Setup();

        return ValueTask.FromResult(StartupTaskState.Completed);
    }
}