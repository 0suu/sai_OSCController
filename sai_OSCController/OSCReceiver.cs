using SharpOSC;
using Cysharp.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.IO;
using sai_OSCController;
using System;
using System.Threading.Tasks;

public class OSCReceiver
{
    readonly string batFile = "ExitVRChat.bat";
    private GoogleAuthService googleAuthService;
    private GoogleAssistantServiceManager assistantServiceManager;
    private const string AssistantOscAddress = "/avatar/parameters/GoogleAssistantQuery";
    private string assistantDeviceModelId = "";
    private string assistantDeviceInstanceId = "";
    private string assistantLanguageCode = "en-US";

    public OSCReceiver(GoogleAuthService authService, GoogleAssistantServiceManager assistantManager)
    {
        this.googleAuthService = authService;
        this.assistantServiceManager = assistantManager;
        Start();
    }

    public void UpdateAssistantDeviceCredentials(string modelId, string instanceId)
    {
        this.assistantDeviceModelId = modelId;
        this.assistantDeviceInstanceId = instanceId;
        this.assistantServiceManager.SetDeviceCredentials(modelId, instanceId); // Pass through
        Console.WriteLine($"OSCReceiver: Assistant Device Credentials Updated. Model ID: {modelId}, Instance ID: {instanceId}");
    }

    public void UpdateAssistantLanguageCode(string langCode)
    {
        this.assistantLanguageCode = langCode;
        this.assistantServiceManager.SetLanguageCode(langCode); // Pass through
        Console.WriteLine($"OSCReceiver: Assistant Language Code Updated to: {langCode}");
    }

    public void Start()
    {
        Console.WriteLine("Start OSCReceiver");
        // Configuration for Assistant (like Device IDs, language) will be set via Update methods from MainWindow
        try
        {
            _ = StartListenerAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OSCリスナーの起動中にエラーが発生しました: {ex.Message}");
        }
    }
    
    async Task StartListenerAsync()
    {
        HandleOscPacket callback = delegate (OscPacket packet)
        {
            HandleMessage((OscMessage)packet);
        };

        var listener = new UDPListener(9001, callback);
        Console.WriteLine("Listening for OSC messages on port 9001...");
        // Keep listener running, UniTask.Never might be an option if this were a UniTask method
        // For a standard Task, ensure it doesn't complete if listener is meant to run indefinitely
        // The UDPListener itself likely runs on its own thread or uses async IO.
        await Task.Delay(-1); // Keeps this async method alive indefinitely if UDPListener doesn't block.
    }

    void OnHandleExitButton(float value)
    {
        if(value > 0.9f)
        {
            try
            {
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string batFilePath = Path.Combine(exeDirectory, batFile);

                if (File.Exists(batFilePath))
                {
                    ProcessStartInfo processInfo = new ProcessStartInfo
                    {
                        FileName = batFilePath,
                        WorkingDirectory = exeDirectory,
                        CreateNoWindow = false,
                        UseShellExecute = false
                    };

                    using (Process process = new Process { StartInfo = processInfo })
                    {
                        Console.WriteLine("バッチファイルを実行します。");
                        process.Start();
                    }
                }
                else
                {
                     Console.WriteLine($"バッチファイルが見つかりません: {batFilePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"バッチファイルの実行中にエラーが発生しました: {ex.Message}");
            }
        }
    }

    void OnHandleAvatarMover(string address, float value)
    {
        var type = AvatarMover.InputType.Unknown;

        switch (address)
        {
            case "/avatar/parameters/AMForward":
                type = AvatarMover.InputType.MoveForward;
                break;
            case "/avatar/parameters/AMBack":
                type = AvatarMover.InputType.MoveBackward;
                break;
            case "/avatar/parameters/AMRight":
                type = AvatarMover.InputType.MoveRight;
                break;
            case "/avatar/parameters/AMLeft":
                type = AvatarMover.InputType.MoveLeft;
                break;
            case "/avatar/parameters/AMJump":
                type = AvatarMover.InputType.Jump;
                break;
            case "/avatar/parameters/AMLookRight":
                type = AvatarMover.InputType.LookRight;
                break;
            case "/avatar/parameters/AMLookLeft":
                type = AvatarMover.InputType.LookLeft;
                break;
            case "/avatar/parameters/AMMic":
                type = AvatarMover.InputType.Voice;
                break;
        }

        MainWindow.Instance?.AvatarMover?.Move(type, value);
    }
    
    private async Task HandleAssistantServiceOsc(OscMessage message)
    {
        if (googleAuthService == null)
        {
            Console.WriteLine("GoogleAuthService is not initialized. Cannot process Assistant command.");
            return;
        }
        if (assistantServiceManager == null)
        {
            Console.WriteLine("GoogleAssistantServiceManager is not initialized. Cannot process Assistant command.");
            return;
        }

        if (string.IsNullOrEmpty(assistantDeviceModelId) || string.IsNullOrEmpty(assistantDeviceInstanceId))
        {
            Console.WriteLine("Assistant Device Model ID or Instance ID is not configured. Cannot process Assistant command.");
            return;
        }

        string accessToken = null;
        try
        {
            accessToken = await googleAuthService.GetAccessTokenAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obtaining access token: {ex.Message}");
            return;
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            Console.WriteLine("Failed to obtain access token. Cannot process Assistant command.");
            return;
        }

        if (message.Arguments.Count == 0 || !(message.Arguments[0] is string textCommand) || string.IsNullOrWhiteSpace(textCommand))
        {
            Console.WriteLine($"Received Assistant OSC message at {message.Address} but found no valid text command in arguments.");
            return;
        }

        Console.WriteLine($"Received command for Google Assistant Service: {textCommand}");

        try
        {
            string responseText = await assistantServiceManager.SendTextQueryAsync(textCommand, accessToken);
            Console.WriteLine($"Google Assistant Response: {responseText}");
            // Here you could potentially send a response back via OSC or update UI
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during Google Assistant query: {ex.Message}");
        }
    }

    void HandleMessage(OscMessage message)
    {
        if (message == null || string.IsNullOrEmpty(message.Address))
        {
            return;
        }

        if (message.Address.Equals(AssistantOscAddress, StringComparison.OrdinalIgnoreCase))
        {
            _ = HandleAssistantServiceOsc(message);
            return;
        }

        // Existing handlers
        if (!message.Address.Contains("Button") && !message.Address.Contains("AM"))
        {
            return;
        }

        Console.WriteLine("------------------------");
        message.Arguments.ForEach(arg =>
        {
            Console.WriteLine($"[{message.Address}] : -> {arg}");

            if (message.Address.Contains("avatar/parameters/ExitButton"))
            {
                OnHandleExitButton((float)arg);
            }
            else if (message.Address.Contains("avatar/parameters/AM"))
            {
                OnHandleAvatarMover(message.Address, (float)arg);
            }
        });
    }
}
