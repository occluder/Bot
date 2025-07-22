using System.Data;
using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;
using Bot.Utils;
using Npgsql;

namespace Bot.StartupTasks;

public class NpgsqlSetup: IStartupTask
{
    public static async Task<IDbConnection> NewDbConnection()
    {
        var conn = new NpgsqlConnection(Environment.GetEnvironmentVariable("PSQL_TWITCHBOT_STRING"));
        await conn.OpenAsync();
        return conn;
    }

    public ValueTask<StartupTaskState> Run()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new JsonTypeHandler<InMemorySettings>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<string[]>());
        return ValueTask.FromResult(StartupTaskState.Completed);
    }
}