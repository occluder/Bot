using System.Data;
using Bot.Enums;
using Bot.Interfaces;
using Npgsql;

namespace Bot.StartupTasks;

public class NpgsqlSetup: IStartupTask
{
    public static SemaphoreSlim LiveConnectionLock { get; } = new(1);
    public static IDbConnection LiveDbConnection { get; set; } = default!;
    public static async Task<IDbConnection> NewDbConnection()
    {
        var conn = new NpgsqlConnection(Config.Secrets["DbConnectionString"]);
        await conn.OpenAsync();
        return conn;
    }

    public async ValueTask<StartupTaskState> Run()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        try
        {
            var conn = new NpgsqlConnection(Config.Secrets["DbConnectionString"]);
            await conn.OpenAsync();
            LiveDbConnection = conn;
        }
        catch (Exception ex)
        {
            ForContext<NpgsqlSetup>().Fatal(ex, "[{ClassName}] Failed to setup Npgsql");
            return StartupTaskState.Failed;
        }

        ForContext<NpgsqlSetup>().Information("Connected to Postgres database");

        return StartupTaskState.Completed;
    }
}