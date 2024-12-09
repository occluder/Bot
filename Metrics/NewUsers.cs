using Bot.Interfaces;

namespace Bot.Metrics;

public class NewUsers: IMetric
{
    private const string KEY = "bot:metrics:users";
    private uint _invc;
    private readonly long _start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public async Task Report()
    {
        if (++_invc % 20 != 0)
            return;

        int count = 0;
        await LiveConnectionLock.WaitAsync();
        try
        {
            dynamic result = await LiveDbConnection.QueryFirstAsync(
                "select count(*) from users where added_at > @AddedAt",
                new
                {
                    AddedAt = _start
                }
            );

            count = (int)result.count;
        }
        catch (Exception ex)
        {
            ForContext<NewUsers>().Error(ex, "Something went wrong");
        }
        finally
        {
            LiveConnectionLock.Release();
        }

        await RedisDatabaseAsync.StringSetAsync(KEY, count, TimeSpan.FromHours(6));
    }
}