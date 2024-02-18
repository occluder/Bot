using Bot.Enums;
using Bot.Interfaces;
using Microsoft.Extensions.Logging;
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