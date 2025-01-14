using System.Data;
using Bot.Enums;
using Bot.Interfaces;
using Npgsql;

namespace Bot.StartupTasks;

public class NpgsqlSetup: IStartupTask
{
    public static async Task<IDbConnection> NewDbConnection()
    {
        var conn = new NpgsqlConnection(Config.Secrets["DbConnectionString"]);
        await conn.OpenAsync();
        return conn;
    }

    public ValueTask<StartupTaskState> Run()
    {
        return ValueTask.FromResult(StartupTaskState.Completed);
    }
}