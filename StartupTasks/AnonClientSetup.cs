using Bot.Enums;
using Bot.Interfaces;
using Microsoft.Extensions.Logging;
using MiniTwitch.Common.Extensions;
using MiniTwitch.Irc;

namespace Bot.StartupTasks;

internal class AnonClientSetup: IStartupTask
{
    public static IrcClient AnonClient { get; private set; } = default!;

    public async ValueTask<StartupTaskState> Run()
    {
        AnonClient = new(options =>
        {
            options.Anonymous = true;
            options.JoinRateLimit = 100;
            options.Logger = new LoggerFactory().AddSerilog(ForContext("IsSubLogger", true).ForContext("Client", "Anon")).CreateLogger<IrcClient>();
        });

        AnonClient.ExceptionHandler = exception =>
        {
            if (exception.StackTrace?.Contains("b__34_0()") is true)
                AnonClient.ReconnectAsync().StepOver();
            else
                ForContext<AnonClientSetup>().Error(exception, "Exception in anonymous client");
        };
        bool connected = await AnonClient.ConnectAsync();
        if (!connected)
        {
            ForContext<AnonClientSetup>().Fatal("[{ClassName}] Failed to setup AnonClient");
            return StartupTaskState.Failed;
        }

        Information("AnonClient setup done");
        return StartupTaskState.Completed;
    }
}