using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Bot.Utils;

internal static class GlobalHelpers
{
    private static readonly HttpClient _httpClient = new();

    public static long Unix() => DateTimeOffset.Now.ToUnixTimeSeconds();

    public static long UnixMs() => DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public static async Task<OneOf<T, HttpStatusCode, Exception>> GetFromRequest<T>(string url,
        JsonSerializerOptions? options = null, [CallerFilePath] string caller = default!,
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
        if (time.Hours > 0) return $"{time:h'h 'mm'm 'ss's'}";
        return time.Minutes > 0 ? $"{time:mm'm 'ss's'}" : $"{time:ss's'}";
    }
}