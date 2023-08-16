using Bot.Enums;
using Bot.Handlers;
using Bot.Workflows;

namespace Bot;

internal class Program
{
    static async Task Main()
    {
        var runner = new WorkflowRunner()
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
           .Add<LoadModules>()
           .Add<InitHandlers>();

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

            var commandResult = await ChatHandler.HandleConsoleCommand(input);
            commandResult.Switch(Console.WriteLine,
            error =>
            {
                Console.WriteLine($"🚨 {error.Value}");
            });
        }
    }
}
