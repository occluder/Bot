using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bot.Models;
using Bot.Services;
using MiniTwitch.Irc.Models;

namespace Bot.Modules;

internal partial class REPL: BotModule
{
    private const int GLOBAL_COOLDOWN_SECONDS = 5;
    private const int USER_COOLDOWN_SECONDS = 15;
    private const int MAX_MESSAGE_LENGTH = 450;

    private readonly JsonSerializerOptions _jsop = new() { WriteIndented = true };
    private readonly Dictionary<long, StringBuilder> _builders = new();
    private readonly Dictionary<long, long> _cooldowns = new();
    private readonly HttpClient _requests = new();
    private DateTime _lastUsed = DateTime.Now;
    private long _globalCooldown = 0;

    public REPL()
    {
        _requests.DefaultRequestHeaders.Add("Authorization", Config.Secrets["EvalAuth"]);
        _requests.Timeout = TimeSpan.FromSeconds(3);
    }

    private async ValueTask OnMessage(Privmsg message)
    {
        bool verbose = false;
        long timeNow = DateTimeOffset.Now.ToUnixTimeSeconds();
        if (message.Channel.Id is not 11148817 and not 780092850 || BlackListedUserIds.Contains(message.Author.Id))
            return;

        if (message.Content.Length < 5 || !PrefixRegex().IsMatch(message.Content))
            return;

        if (message.Content[3] == 'v' && message.Content.Length < 6)
            return;
        else if (message.Content[3] == 'v')
            verbose = true;

        if ((DateTime.Now - _lastUsed).TotalMinutes > 10)
        {
            _builders.Clear();
            _cooldowns.Clear();
        }

        _lastUsed = DateTime.Now;
        string currentContent = message.Content[(verbose ? 5 : 4)..];
        if (message.Content[2] == '?')
        {
            if (_builders.TryGetValue(message.Author.Id, out StringBuilder? sb))
            {
                _ = sb.AppendLine(currentContent);
                return;
            }

            _builders[message.Author.Id] = new();
            _ = _builders[message.Author.Id].AppendLine(currentContent);
            return;
        }

        if (timeNow - _globalCooldown <= TimeSpan.FromSeconds(GLOBAL_COOLDOWN_SECONDS).TotalSeconds)
            return;

        if (_cooldowns.TryGetValue(message.Author.Id, out long lastTime) && timeNow - lastTime <= TimeSpan.FromSeconds(USER_COOLDOWN_SECONDS).TotalSeconds)
            return;

        _globalCooldown = timeNow;
        _cooldowns[message.Author.Id] = timeNow;
        ILogger logger = MessageContextLogger(message);
        string payloadContent = _builders.TryGetValue(message.Author.Id, out StringBuilder? sb2) && sb2.Length > 0 ? GetAndClear(sb2.AppendLine(currentContent)) : currentContent;
        StringContent payload = new(payloadContent);
        HttpResponseMessage response;
        ReplResult? result;
        try
        {
            response = await _requests.PostAsync(Config.Links["Eval"], payload);
            if (response.StatusCode is not HttpStatusCode.OK and not HttpStatusCode.BadRequest)
            {
                logger.Warning("[{Result}] POST {Address}", response.StatusCode, Config.Links["Eval"]);
                await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name},🚫 {response.StatusCode}");
                return;
            }

            logger.Debug("[{Result}] POST {Address}", response.StatusCode, Config.Links["Eval"]);
            result = await response.Content.ReadFromJsonAsync<ReplResult>();
        }
        catch (JsonException jex)
        {
            logger.Error(jex, "JSON deserialization error");
            await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name}, 🚨 Parsing JSON failed.");
            return;
        }
        catch (OperationCanceledException oce)
        {
            logger.Error(oce, "REPL timed out");
            await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name}, 🚨 Operation timed out.");
            return;
        }
        catch (Exception e)
        {
            logger.Error(e, "Exception");
            await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name}, 🚨 An error has occured while running your code.");
            return;
        }

        if (result is null)
        {
            logger.Error("Result is null");
            await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name}, 🚨 Parsing JSON failed.");
            return;
        }

        if (result.ExceptionType is not null)
        {
            string asJson = JsonSerializer.Serialize(result, _jsop);
            string? hasteLink = await UploadToHaste(asJson);
            await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name}, ❌ {result.ExceptionType}: {result.Exception} | {hasteLink}.json");
            return;
        }

        if (verbose)
        {
            string asJson = JsonSerializer.Serialize(result, _jsop);
            string? hasteLink = await UploadToHaste(asJson);
            if (hasteLink is not null)
                await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name}, 🔗 {hasteLink}.json");
            else
                await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name}, Failed to upload the results to Haste");

            return;
        }

        if (result.ConsoleOut is { Length: > 0 } cout)
        {
            if (cout.Length > MAX_MESSAGE_LENGTH)
            {
                string? hasteLink = await UploadToHaste(cout);
                if (hasteLink is not null)
                    await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name}, ✅ Response too long: {hasteLink}");
                else
                    await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name}, ✅ Compilation was successful, but the result is long. Failed to upload the results to Haste");
            }

            await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name}, ✅ ConsoleOutput: {cout}");
            return;
        }

        if (result.ReturnValue?.ToString() is string { Length: > MAX_MESSAGE_LENGTH } longReturnValue)
        {
            string? hasteLink = await UploadToHaste(longReturnValue);
            if (hasteLink is not null)
                await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name}, ✅ Response too long: {hasteLink}");
            else
                await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name}, ✅ Compilation was successful, but the result is long. Failed to upload the results to Haste");
        }

        await MainClient.SendMessage(message.Channel.Name, $"@{message.Author.Name}, ✅ {result.ReturnValue?.ToString()}");
    }

    private async Task<string?> UploadToHaste(string data)
    {
        OneOf<string, Exception> response = await TextUploadService.UploadToHaste(data);
        return response.Match(success => success,
            failure =>
        {
            ForContext<REPL>()
                .ForContext("Data", data)
                .Error(failure, "Failed to upload data to haste");

            return null!;
        });
    }

    [GeneratedRegex("^[cC]#[!?]v? ")]
    private partial Regex PrefixRegex();

    private static ILogger MessageContextLogger(Privmsg message)
    {
        return ForContext<REPL>()
            .ForContext("Privmsg.Content", message.Content)
            .ForContext("Privmsg.Author.Name", message.Author.Name)
            .ForContext("Privmsg.Author.Id", message.Author.Id)
            .ForContext("Privmsg.Channel.Name", message.Channel.Name)
            .ForContext("Privmsg.SentTimestamp", message.SentTimestamp)
            .ForContext("ShowProperties", true);
    }

    private static string GetAndClear(StringBuilder sb)
    {
        string s = sb.ToString();
        _ = sb.Clear();
        return s;
    }

    protected override ValueTask OnModuleEnabled()
    {
        MainClient.OnMessage += OnMessage;
        return default;
    }
    protected override ValueTask OnModuleDisabled()
    {
        MainClient.OnMessage -= OnMessage;
        return default;
    }
}
