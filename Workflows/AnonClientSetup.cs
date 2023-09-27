global using static Bot.Workflows.AnonClientSetup;
using Bot.Enums;
using Bot.Interfaces;
using Microsoft.Extensions.Logging;
using MiniTwitch.Common.Extensions;
using MiniTwitch.Irc;

namespace Bot.Workflows;

internal class AnonClientSetup: IWorkflow
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

        AnonClient.ExceptionHandler = exception =>
        {
            if (exception.GetType() == typeof(KeyNotFoundException))
                AnonClient.ReconnectAsync().StepOver();
            else
                ForContext<AnonClientSetup>().Error(exception, "Exception in anonymous client");
        };
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