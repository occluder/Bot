using Bot.Enums;
using Bot.Interfaces;
using Microsoft.Extensions.Logging;
using MiniTwitch.Irc;

namespace Bot.StartupTasks;

internal class MainClientSetup: IStartupTask
{
    public static IrcClient MainClient { get; private set; } = default!;

    public async ValueTask<StartupTaskState> Run()
    {
        MainClient = new(options =>
        {
            options.Username = Config.Secrets["BotUsername"];
            options.OAuth = Config.Secrets["BotToken"];
            options.Logger = new LoggerFactory().AddSerilog(ForContext("IsSubLogger", true).ForContext("Client", "Main")).CreateLogger<IrcClient>();
        });

        bool connected = await MainClient.ConnectAsync();
        if (!connected)
        {
            ForContext<MainClientSetup>().Fatal("[{ClassName}] Failed to setup MainClient");
            return StartupTaskState.Failed;
        }

        ForContext<MainClientSetup>().Information("MainClient setup done");
        return StartupTaskState.Completed;
    }
}