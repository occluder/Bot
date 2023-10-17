using Bot.Enums;
using Bot.Interfaces;
using Bot.Utils;
using Microsoft.Extensions.Logging;
using MiniTwitch.Irc;

namespace Bot.Workflows;

internal class MainClientSetup: IWorkflow
{
    public static IrcClient MainClient { get; private set; } = default!;

    public async ValueTask<WorkflowState> Run()
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
            return WorkflowState.Failed;
        }

        ForContext("Version", typeof(IrcClient).GetAssemblyVersion()).ForContext("ShowProperties", true)
            .Information("MainClient setup done");
        return WorkflowState.Completed;
    }
}