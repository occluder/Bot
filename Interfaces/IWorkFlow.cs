using Bot.Enums;

namespace Bot.Interfaces;

public interface IWorkflow
{
    public ValueTask<WorkflowState> Run();
}