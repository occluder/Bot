global using static Bot.Workflows.RedisSetup;
using System.Text.Json.Serialization;
using Bot.Enums;
using Bot.Interfaces;
using Bot.Utils;
using CachingFramework.Redis;
using CachingFramework.Redis.Contracts.Providers;
using CachingFramework.Redis.Serializers;
using StackExchange.Redis;

namespace Bot.Workflows;

public class RedisSetup: IWorkflow
{
    public static ICacheProviderAsync Cache { get; private set; } = default!;
    public static ICollectionProvider Collections { get; private set; } = default!;
    public static IPubSubProviderAsync PubSub { get; private set; } = default!;
    public static IDatabaseAsync RedisDatabaseAsync { get; private set; } = default!;

    public async ValueTask<WorkflowState> Run()
    {
        RedisContext context;
        try
        {
            ConnectionMultiplexer multiplexer = await ConnectionMultiplexer.ConnectAsync($"{Config.Links["Redis"]},password={Config.Secrets["RedisPass"]}");
            context = new(multiplexer, new JsonSerializer(new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
            RedisDatabaseAsync = multiplexer.GetDatabase(1);
        }
        catch (Exception)
        {
            ForContext<RedisSetup>().Fatal("[{ClassName}] Failed to setup Redis");
            return WorkflowState.Failed;
        }

        Cache = context.Cache;
        Collections = context.Collections;
        PubSub = context.PubSub;
        ForContext("Version", typeof(RedisContext).GetAssemblyVersion()).ForContext("ShowProperties", true).
            Information("Connected to Redis");

        return WorkflowState.Completed;
    }
}