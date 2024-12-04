using SharpOSC;
using Cysharp.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.IO;
using sai_OSCController;

public class OSCReceiver
{
    readonly string batFile = "ExitVRChat.bat";

    public OSCReceiver()
    {
        Start();
    }

    public void Start()
    {
        Console.WriteLine("Start OSCReceiver");

        try
        {
            RunOSCListenerAsync().Forget();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OSCリスナーの起動中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    async UniTask RunOSCListenerAsync()
    {
        HandleOscPacket callback = delegate (OscPacket packet)
        {
            HandleMessage((OscMessage)packet);
        };

        var listener = new UDPListener(9001, callback);

        Console.WriteLine("Listening for OSC messages...");
    }

    void OnHandleExitButton(float value)
    {
        if(value > 0.9f)
        {
            try
            {
                // 実行ファイル (.exe) と同じディレクトリにあるバッチファイル名
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string batFilePath = Path.Combine(exeDirectory, batFile);

                if (File.Exists(batFilePath))
                {
                    ProcessStartInfo processInfo = new ProcessStartInfo
                    {
                        FileName = batFilePath,
                        WorkingDirectory = exeDirectory,
                        CreateNoWindow = false, // コンソールウィンドウを非表示にする場合は true
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
                    Console.WriteLine("指定されたバッチファイルが存在しません。", "ファイルが見つかりません", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"バッチファイルの実行中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
    
    void HandleMessage(OscMessage message)
    {
        if (message == null)
        {
            return;
        }

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

            if (message.Address.Contains("avatar/parameters/AM"))
            {
                OnHandleAvatarMover(message.Address, (float)arg);
            }
        });
    }
}
