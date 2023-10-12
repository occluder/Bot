using Bot.Enums;
using Bot.Utils.Logging;
using Bot.Workflows;
using Serilog.Events;

namespace Bot;

internal class Program
{
    private static async Task Main()
    {
        WorkflowRunner runner = new WorkflowRunner()
           .Add<LoadConfig>()
           .Add<LoggerSetup>()
           .Add<RedisSetup>()
           .Add<NpgsqlSetup>()
           .Add<LoadInMemorySettings>()
           .Add<MainClientSetup>()
           .Add<AnonClientSetup>()
           .Add<ChannelsSetup>()
           .Add<PubSubSetup>()
           .Add<LoadWhiteListBlackList>()
           .Add<CreateHelixClient>()
           .Add<LoadModules>()
           .Add<InitHandlers>()
           .Add<StartMetrics>();

        await foreach (WorkflowState result in runner.RunAll())
        {
            if (result != WorkflowState.Completed)
                throw new NotSupportedException(result.ToString());
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
            // else if (input == "l!unfilter")
            // {
            //     ClassNameFilter.ClassName = null;
            //     Console.WriteLine("Removed all filters");
            // }
        }
    }
}
