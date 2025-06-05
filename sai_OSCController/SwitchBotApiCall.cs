using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;

namespace SB
{
    public class SwitchBotAPICall
    {
        const string BaseUrl = "https://api.switch-bot.com/v1.1/devices/";
        const string StatusUrl = "/status";

        readonly string token = Environment.GetEnvironmentVariable("sai-osc-SBToken", EnvironmentVariableTarget.Machine);
        readonly string secret = Environment.GetEnvironmentVariable("sai-osc-SBSecret", EnvironmentVariableTarget.Machine);

        public void GetDeviceStatus(string deviceID, Action<string>? callback = null)
        {
            SendRequest($"{BaseUrl}{deviceID}{StatusUrl}", callback).Forget();
        }

        async UniTask SendRequest(string uri, Action<string>? callback)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(secret))
            {
                Console.WriteLine("環境変数が設定されていません");
                return;
            }

            long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string nonce = Guid.NewGuid().ToString();
            string data = token + time + nonce;
            string signature = Convert.ToBase64String(
                new HMACSHA256(Encoding.UTF8.GetBytes(secret)).ComputeHash(Encoding.UTF8.GetBytes(data)));

            using HttpClient client = new();
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.TryAddWithoutValidation("Authorization", token);
            request.Headers.TryAddWithoutValidation("sign", signature);
            request.Headers.TryAddWithoutValidation("nonce", nonce);
            request.Headers.TryAddWithoutValidation("t", time.ToString());

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            callback?.Invoke(await response.Content.ReadAsStringAsync());
        }
    }
}

