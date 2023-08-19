using Bot.Interfaces;
using OneOf.Types;

namespace Bot.Commands.Console;

public class NotFoundCommand : IConsoleCommand
{
    public string Name { get; } = Guid.NewGuid().ToString();

    public ValueTask<OneOf<string, Error<string>>> Execute(string[] args) => ValueTask.FromResult<OneOf<string, Error<string>>>("Command not found");
}
