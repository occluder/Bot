using Bot.Enums;
using Bot.Interfaces;
using Bot.Services;
using Microsoft.Extensions.Logging;
using MiniTwitch.Helix;

namespace Bot.Workflows;

public class CreateHelixClient: IWorkflow
{
    public ValueTask<WorkflowState> Run()
    {
        HelixApi.Client = new HelixClient(
            Config.Secrets["BotToken"],
            Config.Secrets["BotClientId"],
            new LoggerFactory().AddSerilog(Logger).CreateLogger<HelixClient>());

        return ValueTask.FromResult(WorkflowState.Completed);
    }
}