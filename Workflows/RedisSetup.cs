global using static Bot.Workflows.RedisSetup;
using System.Text.Json.Serialization;
using Bot.Enums;
using Bot.Interfaces;
using CachingFramework.Redis;
using CachingFramework.Redis.Contracts.Providers;
using CachingFramework.Redis.Serializers;
using StackExchange.Redis;

namespace Bot.Workflows;

public class RedisSetup : IWorkflow
{
    public static ICacheProviderAsync Cache { get; private set; } = default!;
    public static ICollectionProvider Collections { get; private set; } = default!;
    public static IPubSubProviderAsync PubSub { get; private set; } = default!;

    public async ValueTask<WorkflowState> Run()
    {
        RedisContext context;
        try
        {
            ConnectionMultiplexer multiplexer = await ConnectionMultiplexer.ConnectAsync($"{Config.RedisAddress},password={Config.RedisPass}");
            context = new(multiplexer, new JsonSerializer(new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
        }
        catch (Exception)
        {
            ForContext<RedisSetup>().Fatal("[{ClassName}] Failed to setup Redis");
            return WorkflowState.Failed;
        }

        Information("Connected to Redis");
        Cache = context.Cache;
        Collections = context.Collections;
        PubSub = context.PubSub;
        return WorkflowState.Completed;
    }
}