using Bot.Enums;
using Bot.StartupTasks;
using Bot.Utils;
using Bot.Utils.Logging;
using Serilog.Events;

namespace Bot;

internal class Program
{
    private static async Task Main()
    {
        AppContext.SetSwitch("System.Net.DisableIPv6", true);
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new JsonTypeHandler<LoadInMemorySettings>());

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

        if (Environment.GetEnvironmentVariable("SERVICE") is "TRUE")
        {
            Console.WriteLine("Running as a service -- Console input is disabled.");
            await Task.Delay(-1);
            return;
        }

        while (true)
        {
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
                continue;

            if (Enum.TryParse(input, true, out LogEventLevel level))
            {
                await Settings.ChangeLogLevel((int)level);
                Console.WriteLine($"Switching logging level to: {level}");
            }
            else if (input == "clear")
            {
                Console.Clear();
            }
            else if (input.StartsWith("l!include"))
            {
                string[] args = input.Split(' ');
                if (args.Length < 2)
                    continue;

                ClassNameFilter.ClassName = args[1];
                Console.WriteLine($"ClassNameFilter set to {args[1]}");
            }
            else if (input == "l!unfilter")
            {
                ClassNameFilter.ClassName = null;
                Console.WriteLine("Removed all filters");
            }
        }
    }
}