using Bot.Enums;
using Bot.StartupTasks;

namespace Bot;

internal class Program
{
    private static async Task Main()
    {
        AppContext.SetSwitch("System.Net.DisableIPv6", true);

        StartupTaskRunner runner = new StartupTaskRunner()
            .Add<LoadConfig>()
            .Add<LoggerSetup>()
            .Add<RedisSetup>()
            .Add<NpgsqlSetup>()
            .Add<LoadInMemorySettings>()
            .Add<MainClientSetup>()
            .Add<AnonClientSetup>()
            .Add<FetchPermissions>()
            .Add<ChannelsSetup>()
            .Add<PubSubSetup>()
            .Add<CreateHelixClient>()
            .Add<LoadModules>()
            .Add<InitHandlers>()
            .Add<StartMetrics>();

        await foreach (StartupTaskState result in runner.RunAll())
            if (result != StartupTaskState.Completed)
                throw new NotSupportedException(result.ToString());

        await Task.Delay(-1);
    }
}