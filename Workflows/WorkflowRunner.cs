using Bot.Enums;
using Bot.Interfaces;

namespace Bot.Workflows;
internal class WorkflowRunner
{
    private SortedList<int, IWorkflow> _workflows = new(32);

    public WorkflowRunner Add<TWorkflow>()
        where TWorkflow : IWorkflow, new()
    {
        _workflows.Add(_workflows.Count, new TWorkflow());
        return this;
    }

    public async IAsyncEnumerable<WorkflowState> RunAll()
    {
        foreach (IWorkflow workflow in _workflows.Values)
        {
            yield return await workflow.Run();
        }
    }
}
