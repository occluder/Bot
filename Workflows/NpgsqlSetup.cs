global using Dapper;
global using static Bot.Workflows.NpgsqlSetup;
using System.Data;
using Bot.Enums;
using Bot.Interfaces;
using Bot.Utils;
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
            var conn = new NpgsqlConnection(Config.Secrets["DbConnectionString"]);
            await conn.OpenAsync();
            Postgres = conn;
        }
        catch (Exception ex)
        {
            ForContext<NpgsqlSetup>().Fatal(ex, "[{ClassName}] Failed to setup Npgsql");
            return WorkflowState.Failed;
        }

        ForContext("Version", typeof(NpgsqlConnection).GetAssemblyVersion()).ForContext("ShowProperties", true)
            .Information("Connected to Postgres database");

        return WorkflowState.Completed;
    }
}