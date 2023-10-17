using System.Data;
using Bot.Enums;
using Bot.Interfaces;
using Bot.Utils;
using Npgsql;

namespace Bot.StartupTasks;

public class NpgsqlSetup: IStartupTask
{
    public static IDbConnection Postgres { get; private set; } = default!;
    public static SemaphoreSlim PostgresQueryLock { get; } = new(1);

    public async ValueTask<StartupTaskState> Run()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        try
        {
            var conn = new NpgsqlConnection(Config.Secrets["DbConnectionString"]);
            await conn.OpenAsync();
            Postgres = conn;
        }
        catch (Exception ex)
        {
            ForContext<NpgsqlSetup>().Fatal(ex, "[{ClassName}] Failed to setup Npgsql");
            return StartupTaskState.Failed;
        }

        ForContext("Version", typeof(NpgsqlConnection).GetAssemblyVersion()).ForContext("ShowProperties", true)
            .Information("Connected to Postgres database");

        return StartupTaskState.Completed;
    }
}