using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class GoogleAssistantApi
{
    readonly string projectId = Environment.GetEnvironmentVariable("GA_PROJECT_ID");
    readonly string modelId = Environment.GetEnvironmentVariable("GA_MODEL_ID");
    readonly string deviceId = Environment.GetEnvironmentVariable("GA_DEVICE_ID");
    readonly string accessToken = Environment.GetEnvironmentVariable("GA_ACCESS_TOKEN");

    string Endpoint => $"https://embeddedassistant.googleapis.com/v1alpha2/projects/{projectId}/deviceModels/{modelId}/devices/{deviceId}:assist";

    readonly HttpClient httpClient = new HttpClient();

    public async Task<string?> SendTextQueryAsync(string text)
    {
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(projectId))
        {
            Console.WriteLine("Google Assistant environment variables are not set.");
            return null;
        }

        var body = new
        {
            input = new { text = text },
            dialog_state_in = new { language_code = "ja-JP" },
            device_config = new { device_id = deviceId, device_model_id = modelId }
        };

        var json = JsonConvert.SerializeObject(body);
        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var res = await httpClient.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            Console.WriteLine($"Google Assistant Error: {(int)res.StatusCode} {res.ReasonPhrase}");
            var err = await res.Content.ReadAsStringAsync();
            Console.WriteLine(err);
            return null;
        }

        return await res.Content.ReadAsStringAsync();
    }
}
