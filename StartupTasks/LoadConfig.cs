using Bot.Enums;
using Bot.Interfaces;
using Bot.Models;
using Microsoft.Extensions.Configuration;

namespace Bot.StartupTasks;

public class LoadConfig: IStartupTask
{
    public static AppConfig Config { get; private set; } = default!;

    public ValueTask<StartupTaskState> Run()
    {
        var builder = new ConfigurationBuilder();
        IConfigurationRoot config = builder.AddJsonFile("Config.json").Build();
        var section = new AppConfig() { SettingsKey = config.GetSection("InMemorySettingsKey").Value ?? "bot:settings" };
        string profile = config.GetSection("Profile").Value ?? throw new ArgumentException("Config profile was not set");
        config.Bind(profile, section);
        Config = section;

        return ValueTask.FromResult(StartupTaskState.Completed);
    }
}