global using static Bot.Workflows.LoadWhiteListBlackList;
using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;

namespace Bot.Workflows;

internal class LoadWhiteListBlackList: IWorkflow
{
    public static HashSet<long> WhiteListedUserIds { get; private set; } = default!;
    public static HashSet<long> BlackListedUserIds { get; private set; } = default!;
    private static readonly ILogger _logger = ForContext<LoadWhiteListBlackList>();

    public async ValueTask<WorkflowState> Run()
    {
        try
        {
            WhiteListedUserIds = (await Postgres.QueryAsync<UserDto>("select * from whitelisted_users")).Select(x => x.Id).ToHashSet();
        }
        catch (Exception ex)
        {
            _logger.Information(ex, "[{ClassName}] Loading whitelisted users failed!");
            return WorkflowState.Failed;
        }

        try
        {
            BlackListedUserIds = (await Postgres.QueryAsync<UserDto>("select * from blacklisted_users")).Select(x => x.Id).ToHashSet();
        }
        catch (Exception ex)
        {
            _logger.Information(ex, "[{ClassName}] Loading blacklisted users failed!");
            throw;
        }

        _logger.Information("Loaded {WhiteListCount} whitelisted users, {BlackListCount} blacklisted users", WhiteListedUserIds.Count, BlackListedUserIds.Count);
        return WorkflowState.Completed;
    }
}