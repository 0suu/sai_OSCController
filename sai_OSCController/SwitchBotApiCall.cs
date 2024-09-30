using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Cysharp.Threading.Tasks;

namespace SB
{
    public enum DeviceType
    {
        None,
        Meter,
        Plug,
        PlugMini,
        Bot,
        MotionSensor,
        CeilingLightPro,
        ContactSensor,
        Curtain,
        Remote,
        Humidifier,
        AirConditioner,
    }

    public class Device
    {
        public string ID = null;
        public string Name = null;
        public DeviceType Type = DeviceType.None;

        public Device(string deviceID, string deviceName, DeviceType deviceType)
        {
            this.ID = deviceID;
            this.Name = deviceName;
            this.Type = deviceType;
        }
    }

    public class SwitchBotAPICall
    {
        readonly string SwitchBotBaseURL = "https://api.switch-bot.com/v1.1";
        readonly string DevicesURL = "/devices/";
        readonly string StatusURL = "/status";
        readonly string ScenesURL = "/scenes";
        readonly string contenttype = "application/json";

        string token => Environment.GetEnvironmentVariable("sai-osc-SBToken", EnvironmentVariableTarget.Machine);
        string secret => Environment.GetEnvironmentVariable("sai-osc-SBSecret", EnvironmentVariableTarget.Machine);

        DateTime dt1970 = new DateTime(1970, 1, 1);
        DateTime current = new();
        TimeSpan span = new();
        long time = 0;
        string nonce = "";
        string data = "";
        Encoding utf8 = Encoding.UTF8;
        HMACSHA256 hmac = new();
        string signature = "";

        public static SwitchBotAPICall instance;

        public string _currentDeviceID = null;

        List<Device> DevicesList = new List<Device>();

        public event Action<List<Device>> OnLoadJson = null;

        public bool CompleteFirstRequest = false;

        public SwitchBotAPICall()
        {
            Console.WriteLine("Start SwitchBotApiCall");
            GetRequestFirst().Forget();
        }

        public async UniTask GetRequestFirst()
        {
            Console.WriteLine("初回リクエスト");

            if(token == null || secret == null)
            {
                Console.WriteLine("環境変数が設定されていません");
                return;
            }

            Console.WriteLine(token + secret);

            current = DateTime.Now;
            span = current - dt1970;
            time = Convert.ToInt64(span.TotalMilliseconds);
            nonce = Guid.NewGuid().ToString();
            data = token + time.ToString() + nonce;
            utf8 = Encoding.UTF8;
            hmac = new HMACSHA256(utf8.GetBytes(secret));
            signature = Convert.ToBase64String(hmac.ComputeHash(utf8.GetBytes(data)));

            await GetRequest(SwitchBotBaseURL + DevicesURL, GetDeviceList: true);

            CompleteFirstRequest = true;
        }

        public void OnClickGetDeviceList()
        {
            GetRequest(SwitchBotBaseURL + DevicesURL, true).ContinueWith(_ => {; });
        }

        public void OnClickGetDeviceStatus()
        {
            GetRequest(SwitchBotBaseURL + DevicesURL + _currentDeviceID + StatusURL).ContinueWith(_ => {; });
        }

        public void GetDeviceStatus(Device device, Action<string> callback = null)
        {
            GetRequest(SwitchBotBaseURL + DevicesURL + device.ID + StatusURL, callback: callback).ContinueWith(_ => {; });
        }

        public void GetDeviceStatus(string deviceID, Action<string> callback = null)
        {
            GetRequest(SwitchBotBaseURL + DevicesURL + deviceID + StatusURL, callback: callback).ContinueWith(_ => {; });
        }

        public void CurtainOpen(Device device)
        {
            PostRequestDevices(device, "setPosition", "0,ff,0");
        }

        public void CurtainClose(Device device)
        {
            PostRequestDevices(device, "setPosition", "0,ff,100");
        }

        public void CurtainControl(Device device, string value)
        {
            PostRequestDevices(device, "setPosition", "0,ff," + value);
        }

        public void CeilingLightProControl(Device device, string command, string parameter)
        {
            PostRequestDevices(device, command, parameter);
        }

        public void PlugOn()
        {
            //PostRequestDevices(_currentDeviceID, isOn: true);
        }

        public void PlugOff()
        {
            //PostRequestDevices(_currentDeviceID, isOn: false);
        }

        public void PlugToggle(Device device, string command)
        {
            PostRequestDevices(device, command: command);
        }

        public void CeilingLightOn()
        {
            //PostRequestDevices(SwitchBotDeviceManager.instance._ceilingLightDeviceID, isOn: true);
        }

        public void CeilingLightOff()
        {
            //PostRequestDevices(SwitchBotDeviceManager.instance._ceilingLightDeviceID, isOn: false);
        }

        public void AirConditionerControl(Device device, string parameter)
        {
            PostRequestDevices(device, "setAll", parameter);
        }

        public void PushBot(Device device, string command)
        {
            PostRequestDevices(device, command);
        }

        public void GetSceneList(Action<string> callback = null)
        {
            GetRequest(SwitchBotBaseURL + ScenesURL, callback: callback).ContinueWith(_ => {; });
        }
        /*
        public void ExecuteScene(SceneData sceneData)
        {
            PostSceneRequest(sceneData).ContinueWith(_ => {; });
        }
        */


        [System.Serializable]
        public class PostJson
        {
            public string command;
            public string parameter;
            public string commandType;
        }

        void PostRequestDevices(Device device, string command = "", string parameter = "default", string commandType = "command")
        {
            var Type = device.Type;
            var ID = device.ID;

            PostJson json = new PostJson();

            //string boolCommand = isOn ? "turnOn" : "turnOff";
            //string powerState = isOn ? "on" : "off";

            switch (Type)
            {
                case DeviceType.Meter:
                    break;

                case DeviceType.Plug:
                    json.command = command;
                    json.parameter = parameter;
                    json.commandType = commandType;
                    break;

                case DeviceType.PlugMini:
                    json.command = command;
                    json.parameter = parameter;
                    json.commandType = commandType;
                    break;

                case DeviceType.Curtain:
                    json.command = command;
                    json.parameter = parameter;
                    json.commandType = commandType;
                    break;

                case DeviceType.CeilingLightPro:
                    json.command = command;
                    json.parameter = parameter;
                    json.commandType = commandType;
                    break;

                case DeviceType.Bot:
                    json.command = command;
                    json.parameter = parameter;
                    json.commandType = commandType;
                    break;

                case DeviceType.AirConditioner:
                    json.command = command;
                    json.parameter = parameter;
                    json.commandType = commandType;
                    break;
            }

            Debug.WriteLine($"PostRequest: command: {command} parameter: {parameter}");
            PostRequest(ID, json).ContinueWith(_ => {; });
        }

        public async Task PostRequest(string deviceID, PostJson json)
        {
            string myJson = JsonConvert.SerializeObject(json);
            byte[] postData = Encoding.UTF8.GetBytes(myJson);

            //Create http client
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, @"https://api.switch-bot.com/v1.1/devices/" + deviceID + "/commands");
            request.Headers.TryAddWithoutValidation(@"Authorization", token);
            request.Headers.TryAddWithoutValidation(@"sign", signature);
            request.Headers.TryAddWithoutValidation(@"nonce", nonce);
            request.Headers.TryAddWithoutValidation(@"t", time.ToString());

            var byteContent = new ByteArrayContent(postData);
            request.Content = byteContent;
            var response = await client.SendAsync(request);

            var log = await response.Content.ReadAsStringAsync();

            Debug.WriteLine("Request : " + request);
            Debug.WriteLine(log);
            //SwitchBotControllUIManager.instance.UpdateLogText(log);
        }

        public async Task GetRequest(string uri, bool GetDeviceList = false, Action<string> callback = null)
        {
            try
            {
                // HttpClientの作成
                HttpClient client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.TryAddWithoutValidation(@"Authorization", token);
                request.Headers.TryAddWithoutValidation(@"sign", signature);
                request.Headers.TryAddWithoutValidation(@"nonce", nonce);
                request.Headers.TryAddWithoutValidation(@"t", time.ToString());

                var response = await client.SendAsync(request);

                // 成功レスポンスかどうかを確認
                response.EnsureSuccessStatusCode();

                var output = await response.Content.ReadAsStringAsync();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;

                Console.WriteLine(pages[page] + ":\nReceived: " + output);

                // 認証エラーチェック
                if (output == "{\"message\":\"Unauthorized\"}")
                {
                    Console.WriteLine("認証エラー");
                    return;
                }

                // デバイスリストの取得
                if (GetDeviceList) AddDropdownJson(output);

                callback?.Invoke(output);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"HTTPリクエストエラー: {e.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"エラー: {e.Message}");
            }
        }


        /// <summary>
        /// デバイスリストを成形する
        /// </summary>
        /// <param name="text"></param>
        void AddDropdownJson(string output)
        {
            Debug.WriteLine("デバイスリストを成型する");
            DevicesList.Clear();
            Debug.WriteLine(output);
            JObject parsedJson = JObject.Parse(output);

            JArray deviceList = (JArray)parsedJson["body"]["deviceList"];
            JArray infraredRemoteList = (JArray)parsedJson["body"]["infraredRemoteList"];

            foreach (JObject device in deviceList)
            {
                string deviceID = (string)device["deviceId"];
                string deviceName = (string)device["deviceName"];
                var deviceType = GetDeviceType((string)device["deviceType"]);

                DevicesList.Add(new Device(deviceID, deviceName, deviceType));
            }

            foreach (JObject device in infraredRemoteList)
            {
                string deviceID = (string)device["deviceId"];
                string deviceName = (string)device["deviceName"];
                var deviceType = GetDeviceType((string)device["remoteType"]);

                DevicesList.Add(new Device(deviceID, deviceName, deviceType));
            }

            //SwitchBotControllUIManager.instance.UpdateDevicesDropdown(DevicesList);
            Debug.WriteLine("デバイスリストを成型した");
            //OnLoadJson.Invoke(DevicesList);
        }

        DeviceType GetDeviceType(string deviceType)
        {
            switch (deviceType)
            {
                case "Bot":
                    return DeviceType.Bot;
                case "Ceiling Light Pro":
                    return DeviceType.CeilingLightPro;
                case "Curtain":
                    return DeviceType.Curtain;
                case "Plug Mini (JP)":
                    return DeviceType.PlugMini;
                case "Plug Mini":
                    return DeviceType.PlugMini;
                case "Meter":
                    return DeviceType.Meter;
                case "Plug":
                    return DeviceType.Plug;
                case "Remote":
                    return DeviceType.Remote;
                case "MotionSensor":
                    return DeviceType.MotionSensor;
                case "ContactSensor":
                    return DeviceType.ContactSensor;
                case "Humidifier":
                    return DeviceType.Humidifier;
                case "Air Conditioner":
                    return DeviceType.AirConditioner;
                default:
                    return DeviceType.None;
            }
        }
    }
}
