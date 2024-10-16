//using Cysharp.Threading.Tasks;
using SharpOSC;
using Cysharp.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.IO;

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
            RunOSCListenerAsync();
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
    
    void HandleMessage(OscMessage message)
    {
        if (message == null)
        {
            return;
        }

        if (!message.Address.Contains("Button"))
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
        });
    }
}
