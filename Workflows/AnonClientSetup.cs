global using static Bot.Workflows.AnonClientSetup;
using Bot.Enums;
using Bot.Interfaces;
using Microsoft.Extensions.Logging;
using MiniTwitch.Irc;

namespace Bot.Workflows;

internal class AnonClientSetup : IWorkflow
{
    public static IrcClient AnonClient { get; private set; } = default!;

    public async ValueTask<WorkflowState> Run()
    {
        AnonClient = new(options =>
        {
            options.Anonymous = true;
            options.JoinRateLimit = 100;
            options.Logger = new LoggerFactory().AddSerilog(ForContext("IsSubLogger", true).ForContext("Client", "Anon")).CreateLogger<IrcClient>();
        });

        bool connected = await AnonClient.ConnectAsync();
        if (!connected)
        {
            ForContext<AnonClientSetup>().Fatal("[{ClassName}] Failed to setup AnonClient");
            return WorkflowState.Failed;
        }

        Information("AnonClient setup done");
        return WorkflowState.Completed;
    }
}