using System.Text.Json;
using System.Text.Json.Serialization;
using Bot.Enums;
using Bot.Interfaces;
using Bot.Utils;
using CachingFramework.Redis;
using CachingFramework.Redis.Contracts.Providers;
using StackExchange.Redis;
using JsonSerializer = CachingFramework.Redis.Serializers.JsonSerializer;

namespace Bot.StartupTasks;

public class RedisSetup: IStartupTask
{
    public static ICacheProviderAsync Cache { get; private set; } = default!;
    public static ICollectionProvider Collections { get; private set; } = default!;
    public static IDatabaseAsync RedisDatabaseAsync { get; private set; } = default!;

    public async ValueTask<StartupTaskState> Run()
    {
        RedisContext context;
        try
        {
            ConnectionMultiplexer multiplexer =
                await ConnectionMultiplexer.ConnectAsync(
                    $"{Config.Links["Redis"]},password={Config.Secrets["RedisPass"]}");
            context = new RedisContext(multiplexer,
                new JsonSerializer(new JsonSerializerOptions
                    { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
            RedisDatabaseAsync = multiplexer.GetDatabase(0);
        }
        catch (Exception)
        {
            ForContext<RedisSetup>().Fatal("[{ClassName}] Failed to setup Redis");
            return StartupTaskState.Failed;
        }

        Cache = context.Cache;
        Collections = context.Collections;
        ForContext("Version", typeof(RedisContext).GetAssemblyVersion()).ForContext("ShowProperties", true)
            .Information("Connected to Redis");

        return StartupTaskState.Completed;
    }
}