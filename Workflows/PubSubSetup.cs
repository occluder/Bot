global using static Bot.Workflows.PubSubSetup;
using Bot.Enums;
using Bot.Interfaces;
using Bot.Utils;
using Microsoft.Extensions.Logging;
using MiniTwitch.PubSub;

namespace Bot.Workflows;

internal class PubSubSetup : IWorkflow
{
    public static PubSubClient TwitchPubSub { get; private set; } = default!;

    public async ValueTask<WorkflowState> Run()
    {
        TwitchPubSub = new(Config.Token, new LoggerFactory().AddSerilog(ForContext("IsSubLogger", true).ForContext("Client", "PubSub")).CreateLogger<PubSubClient>());
        if (!await TwitchPubSub.ConnectAsync())
        {
            ForContext<PubSubSetup>().Fatal("[{ClassName}] Failed to setup PubSub");
            return WorkflowState.Failed;
        }

        ForContext("Version", typeof(PubSubClient).GetAssemblyVersion()).ForContext("ShowProperties", true)
            .Information("PubSub setup done");
        return WorkflowState.Completed;
    }
}
