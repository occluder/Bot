using System.Text.Json.Serialization;

namespace Bot.Models;

public record ReplResult(
    [property: JsonPropertyName("returnValue")] object? ReturnValue,
    [property: JsonPropertyName("returnTypeName")] string? ReturnTypeName,
    [property: JsonPropertyName("exception")] string? Exception,
    [property: JsonPropertyName("exceptionType")] string? ExceptionType,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("consoleOut")] string ConsoleOut,
    [property: JsonPropertyName("executionTime")] TimeSpan ExecutionTime,
    [property: JsonPropertyName("compileTime")] TimeSpan CompileTime
);
