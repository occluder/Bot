global using OneOf;
global using static Bot.Utils.GlobalHelpers;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Bot.Utils;

internal static class GlobalHelpers
{
    private static readonly HttpClient _httpClient = new();

    public static async Task<OneOf<T, HttpStatusCode, Exception>> GetFromRequest<T>(string url, JsonSerializerOptions? options = null, [CallerFilePath] string caller = default!, [CallerLineNumber] int line = default)
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

        T? responseValue = await response.Content.ReadFromJsonAsync<T>(options);
        if (responseValue is null)
            return new NullReferenceException("Deserialized value is null");

        return responseValue;
    }
}
