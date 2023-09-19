﻿using Bot.Interfaces;

namespace Bot.Metrics;

public class Users: IMetric
{
    private const string KEY = "tstack:user_count";
    private uint _invc;

    public async Task Report()
    {
        if (++_invc % 100 != 0) return;

        await PostgresQueryLock.WaitAsync();
        try
        {
            int userCount = (int)await Postgres.QuerySingleAsync("select COUNT(*) from collected_users");
            await RedisDatabaseAsync.StringSetAsync(KEY, userCount, TimeSpan.FromHours(6));
        }
        catch (Exception ex)
        {
            ForContext<Users>().Error(ex, "Something went wrong {InvocationCount}", _invc);
        }
        finally
        {
            PostgresQueryLock.Release();
        }
    }
}