global using static Bot.Workflows.NpgsqlSetup;
global using Dapper;
using System.Data;
using Bot.Enums;
using Bot.Interfaces;
using Npgsql;

namespace Bot.Workflows;

public class NpgsqlSetup : IWorkflow
{
    public static IDbConnection Postgres { get; private set; } = default!;
    public static SemaphoreSlim PostgresTimerSemaphore { get; } = new(1);

    public async ValueTask<WorkflowState> Run()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        try
        {
            var conn = new NpgsqlConnection(Config.DbConnectionString);
            await conn.OpenAsync();
            Postgres = conn;
        }
        catch (Exception ex)
        {
            ForContext<NpgsqlSetup>().Fatal(ex, "[{ClassName}] Failed to setup Npgsql");
            return WorkflowState.Failed;
        }

        Information("Connected to Postgres Db");
        return WorkflowState.Completed;
    }
}