using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;

namespace Bot.StartupTasks;

internal class FetchPermissions: IStartupTask
{
    public static Dictionary<long, UserPermissionDto> UserPermissions { get; private set; } = default!;

    private static readonly ILogger _logger = ForContext<FetchPermissions>();

    public async ValueTask<StartupTaskState> Run()
    {
        try
        {
            UserPermissions = (await Postgres.QueryAsync<UserPermissionDto>("select * from user_permissions"))
                .ToDictionary(x => x.UserId);
        }
        catch (Exception ex)
        {
            _logger.Information(ex, "[{ClassName}] Loading user permissions failed!");
            return StartupTaskState.Failed;
        }

        _logger.Information("Loaded {UserCount} users with modified permissions", UserPermissions.Count);
        return StartupTaskState.Completed;
    }
}