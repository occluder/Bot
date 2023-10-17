using Bot.Enums;
using Bot.Interfaces;
using Bot.Utils;
using Microsoft.Extensions.Logging;
using MiniTwitch.PubSub;

namespace Bot.StartupTasks;

internal class PubSubSetup: IStartupTask
{
    public async ValueTask<StartupTaskState> Run()
    {
        TwitchPubSub = new(Config.Secrets["BotToken"],
            new LoggerFactory().AddSerilog(ForContext("IsSubLogger", true)
                    .ForContext("Client", "PubSub"))
                .CreateLogger<PubSubClient>());

        if (!await TwitchPubSub.ConnectAsync())
        {
            ForContext<PubSubSetup>().Fatal("[{ClassName}] Failed to setup PubSub");
            return StartupTaskState.Failed;
        }

        ForContext("Version", typeof(PubSubClient).GetAssemblyVersion()).ForContext("ShowProperties", true)
            .Information("PubSub setup done");

        return StartupTaskState.Completed;
    }
}