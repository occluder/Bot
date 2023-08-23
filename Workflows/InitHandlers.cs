using Bot.Enums;
using Bot.Handlers;
using Bot.Interfaces;

namespace Bot.Workflows;

internal class InitHandlers: IWorkflow
{
    public ValueTask<WorkflowState> Run()
    {
        ChatHandler.Setup();

        return ValueTask.FromResult(WorkflowState.Completed);
    }
}