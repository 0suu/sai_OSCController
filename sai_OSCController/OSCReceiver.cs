using SharpOSC;
using Cysharp.Threading.Tasks; // Keep this if still used elsewhere, or remove if specific to old async model
using System.Diagnostics;
using System.Windows; // For MessageBox, consider if still appropriate here or should be in UI layer
using System.IO;
using sai_OSCController; // Assuming DialogflowManager is in this namespace
using System; // Added for AppDomain, Guid, Exception
using System.Threading.Tasks; // Added for Task

public class OSCReceiver
{
    readonly string batFile = "ExitVRChat.bat";
    private DialogflowManager dialogflowManager; // Added
    private const string DialogflowOscAddressPlaceholder = "/avatar/parameters/GoogleAssistantQuery"; // Added - User will define actual address

    // Placeholder for Project ID - will be loaded from settings later
    private string dialogflowProjectId = ""; // Added

    public OSCReceiver()
    {
        // Initialize DialogflowManager
        try
        {
            dialogflowManager = new DialogflowManager();
            // Load Project ID from settings here eventually
            // For now, we can leave it empty or use a hardcoded placeholder for initial testing if necessary,
            // but the DialogflowManager's SetProjectId should be called before making API calls.
            // Example: LoadDialogflowProjectIdFromSettings();
            // dialogflowManager.SetProjectId(this.dialogflowProjectId); // Call this after loading
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize DialogflowManager: {ex.Message}. Google Assistant functionality will be unavailable.");
            // Decide if the application can continue without Dialogflow or if this is a critical failure
            // For now, we log and continue.
            dialogflowManager = null; // Ensure it's null if initialization fails
        }
        Start();
    }

    // Method to be called when Project ID is loaded/changed
    public void UpdateDialogflowProjectId(string projectId)
    {
        this.dialogflowProjectId = projectId;
        if (dialogflowManager != null)
        {
            dialogflowManager.SetProjectId(this.dialogflowProjectId);
        }
        else
        {
            Console.WriteLine("DialogflowManager is not initialized. Cannot set Project ID.");
        }
    }


    public void Start()
    {
        Console.WriteLine("Start OSCReceiver");
        // Load Project ID from settings when OSCReceiver starts or is configured
        // This is a good place to ensure it's loaded if not done in constructor.
        LoadDialogflowProjectIdFromSettings(); // We'll implement this properly in the configuration step

        try
        {
            RunOSCListenerAsync().Forget(); // Assuming UniTask is still used for this listener
        }
        catch (Exception ex)
        {
            // Using Console.WriteLine for logging as MessageBox might not be ideal for a background listener
            Console.WriteLine($"OSCリスナーの起動中にエラーが発生しました: {ex.Message}");
        }
    }
    
    // Placeholder for loading from settings - will be implemented in a later step
    private void LoadDialogflowProjectIdFromSettings()
    {
        string projectIdFromSettings = sai_OSCController.Properties.Settings.Default.DialogflowProjectId;
        if (!string.IsNullOrEmpty(projectIdFromSettings))
        {
            UpdateDialogflowProjectId(projectIdFromSettings);
            Console.WriteLine($"Loaded Dialogflow Project ID from settings: {projectIdFromSettings}");
        }
        else
        {
            Console.WriteLine("Dialogflow Project ID is not configured in settings. Please configure it for Google Assistant functionality.");
            // UpdateDialogflowProjectId(""); // Explicitly set to empty if not found, ensures DialogflowManager knows
        }
    }

    async UniTask RunOSCListenerAsync() // Assuming UniTask is the desired way to run this
    {
        HandleOscPacket callback = delegate (OscPacket packet)
        {
            // Ensure messages are handled on a thread that can make async calls if necessary,
            // or ensure HandleMessage itself is fully async if it calls async Dialogflow methods.
            // For now, direct call. If issues arise, consider Task.Run or similar.
            HandleMessage((OscMessage)packet);
        };

        var listener = new UDPListener(9001, callback);
        Console.WriteLine("Listening for OSC messages on port 9001...");
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
    
    // New method to handle Dialogflow OSC messages
    private async Task HandleDialogflowOsc(OscMessage message)
    {
        if (dialogflowManager == null)
        {
            Console.WriteLine("DialogflowManager is not initialized. Cannot process Google Assistant command.");
            return;
        }

        if (string.IsNullOrEmpty(this.dialogflowProjectId))
        {
            Console.WriteLine("Dialogflow Project ID is not configured. Cannot process Google Assistant command.");
            // Attempt to load it again, or notify user through UI if possible.
            // LoadDialogflowProjectIdFromSettings(); // Try loading again
            // if (string.IsNullOrEmpty(this.dialogflowProjectId)) return; // Still not loaded
            return;
        }

        if (message.Arguments.Count == 0 || !(message.Arguments[0] is string textCommand) || string.IsNullOrWhiteSpace(textCommand))
        {
            Console.WriteLine($"Received Dialogflow OSC message at {message.Address} but found no valid text command in arguments.");
            return;
        }

        Console.WriteLine($"Received command for Dialogflow: {textCommand}");
        string sessionId = Guid.NewGuid().ToString(); // Unique session ID for each query

        try
        {
            // The DialogflowManager's ProjectId should be set by now via UpdateDialogflowProjectId
            Google.Cloud.Dialogflow.V2.DetectIntentResponse response = await dialogflowManager.DetectIntentAsync(sessionId, textCommand);
            if (response != null)
            {
                Console.WriteLine($"Dialogflow Fulfillment: {response.QueryResult.FulfillmentText}");
                // Here you could potentially send a response back via OSC or update UI
            }
            else
            {
                Console.WriteLine("No response or error from Dialogflow.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during Dialogflow intent detection: {ex.Message}");
        }
    }

    void HandleMessage(OscMessage message)
    {
        if (message == null || string.IsNullOrEmpty(message.Address))
        {
            return;
        }

        // Log all received messages for debugging if needed (optional)
        // Console.WriteLine($"OSC Received: {message.Address} with {message.Arguments.Count} args.");

        // Check for Dialogflow OSC address first
        // Using StartsWith to allow for potential sub-addresses if user configures it that way,
        // but exact match might be better depending on final OSC address definition.
        if (message.Address.Equals(DialogflowOscAddressPlaceholder, StringComparison.OrdinalIgnoreCase))
        {
            // Call the async handler. Don't wait for it here to avoid blocking OSC listener thread.
            // Fire and forget, with error handling inside HandleDialogflowOsc.
            _ = HandleDialogflowOsc(message);
            return; // Message handled
        }

        // Existing handlers
        if (!message.Address.Contains("Button") && !message.Address.Contains("AM"))
        {
            // If it's not Dialogflow and not Button/AM, ignore.
            // Comment out the return to log all messages if needed for debugging.
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
            else if (message.Address.Contains("avatar/parameters/AM")) // Use else if to avoid double processing
            {
                OnHandleAvatarMover(message.Address, (float)arg);
            }
        });
    }
}
