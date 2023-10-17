using Bot.Enums;
using Bot.Interfaces;

namespace Bot.StartupTasks;

internal class StartupTaskRunner
{
    private readonly SortedList<int, IStartupTask> _workflows = new(32);

    public StartupTaskRunner Add<TStartupTask>()
        where TStartupTask : IStartupTask, new()
    {
        _workflows.Add(_workflows.Count, new TStartupTask());
        return this;
    }

    public async IAsyncEnumerable<StartupTaskState> RunAll()
    {
        foreach (IStartupTask workflow in _workflows.Values)
        {
            yield return await workflow.Run();
        }
    }
}
