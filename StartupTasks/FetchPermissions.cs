﻿using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;

namespace Bot.StartupTasks;

internal class FetchPermissions: IStartupTask
{
    private static readonly ILogger _logger = ForContext<FetchPermissions>();
    public static Dictionary<long, UserPermissionDto> UserPermissions { get; private set; } = default!;

    public async ValueTask<StartupTaskState> Run()
    {
        using var conn = await NewDbConnection();
        try
        {
            UserPermissions = (await conn.QueryAsync<UserPermissionDto>("select * from user_permissions"))
                .ToDictionary(x => x.UserId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[{ClassName}] Loading user permissions failed!");
            return StartupTaskState.Failed;
        }

        _logger.Information("Loaded {UserCount} users with modified permissions", UserPermissions.Count);
        return StartupTaskState.Completed;
    }

    public static bool UserBlacklisted(long id) => UserPermissions.ContainsKey(id) && UserPermissions[id].IsBlacklisted;
}