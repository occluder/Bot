using OneOf.Types;

namespace Bot.Interfaces;

public interface IConsoleCommand
{
    string Name { get; }
    ValueTask<OneOf<string, Error<string>>> Execute(string[] args);
}
