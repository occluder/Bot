using Bot.Enums;
using Bot.Interfaces;
using Microsoft.Extensions.Logging;
using MiniTwitch.Helix;

namespace Bot.StartupTasks;

public class CreateHelixClient: IStartupTask
{
    public ValueTask<StartupTaskState> Run()
    {
        HelixClient = new HelixWrapper(
            Config.Secrets["BotToken"],
            Config.Ids["BotId"],
            new LoggerFactory().AddSerilog(Logger).CreateLogger<HelixWrapper>());

        return ValueTask.FromResult(StartupTaskState.Completed);
    }
}