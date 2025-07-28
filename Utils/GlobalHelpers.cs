using System.Drawing;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MiniTwitch.Irc;

namespace Bot.Utils;

internal static class GlobalHelpers
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders =
        {
            { "crossplay", "true" }
        }
    };

    public static uint Unsigned24Color(Color color)
    {
        uint c = 0;
        c |= color.R;
        c <<= 8;
        c |= color.G;
        c <<= 8;
        c |= color.B;
        return c;
    }

    public static long Unix() => DateTimeOffset.Now.ToUnixTimeSeconds();

    public static long UnixMs() => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public static async Task<OneOf<T, HttpStatusCode, Exception>> GetFromRequest<T>(
        string url,
        JsonSerializerOptions? options = null,
        [CallerFilePath] string caller = default!,
        [CallerLineNumber] int line = default)
    {
        ILogger logger = ForContext("CallerFilePath", caller).ForContext("CallerLineNumber", line);
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                logger.Warning("[{StatusCode}] GET {Url}", response.StatusCode, url);
                return response.StatusCode;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "GET {Url}", url);
            return ex;
        }

        logger.Debug("[{StatusCode}] GET {Url}", response.StatusCode, url);
        try
        {
            T? responseValue = await response.Content.ReadFromJsonAsync<T>(options);
            if (responseValue is null)
                return new NullReferenceException("Deserialized value is null");

            return responseValue;
        }
        catch (Exception e)
        {
            logger.Error(e, "GET {Url}", url);
            return e;
        }
    }

    public static string PrettyTimeString(TimeSpan time)
    {
        return time switch
        {
            { Days: > 7 } => $"{time:dd'd'}",
            { Days: > 2 } => $"{time:dd'd 'hh'h'}",
            { Days: > 0 } => $"{time:dd'd 'hh'h 'mm'm'}",
            { Hours: > 0 } => $"{time:h'h 'mm'm 'ss's'}",
            { Minutes: > 0 } => $"{time:mm'm 'ss's'}",
            _ => $"{time:ss's'}"
        };
    }

    public static async ValueTask SendMessage(this IrcClient client, string[] channels, string message, bool action = false, string? nonce = null, CancellationToken cancellationToken = default(CancellationToken))
    {
        foreach (string channel in channels)
        {
            await client.SendMessage(channel, message, action, nonce, cancellationToken);
        }
    }
}