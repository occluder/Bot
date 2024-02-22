using System.Net.Http.Json;
using System.Text;

namespace Bot.Services;

public class TextUploadService
{
    private static readonly HttpClient _requests = new() { Timeout = TimeSpan.FromSeconds(10) };

    public static async Task<OneOf<string, Exception>> UploadToHaste(string data)
    {
        const string link = "https://paste.ivr.fi/documents";
        StringContent content = new(data, Encoding.UTF8);
        HttpResponseMessage response = await _requests.PostAsync(link, content);
        ForContext<TextUploadService>().Debug("[{Result}] POST {Link}", response.StatusCode, link);
        if (!response.IsSuccessStatusCode)
            return new HttpRequestException($"Response code does not indicate success: {(int)response.StatusCode}");

        Dictionary<string, string>? result;
        try
        {
            result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        }
        catch (Exception ex)
        {
            ForContext<TextUploadService>()
                .ForContext("Data", data)
                .ForContext("HasteLink", link)
                .Error(ex, "Failed to upload data to haste");

            return ex;
        }

        if (result?["key"] is string key) return $"https://paste.ivr.fi/raw/{key}";

        return new NullReferenceException();
    }
}