using Bot.Enums;
using Bot.Interfaces;
using Bot.Services;
using Microsoft.Extensions.Logging;
using MiniTwitch.Helix;

namespace Bot.StartupTasks;

public class CreateHelixClient: IStartupTask
{
    public ValueTask<StartupTaskState> Run()
    {
        Helix.HelixClient = new HelixClient(
            Config.Secrets["BotToken"],
            Config.Secrets["BotClientId"],
            new LoggerFactory().AddSerilog(Logger).CreateLogger<HelixClient>());

        return ValueTask.FromResult(StartupTaskState.Completed);
    }
}