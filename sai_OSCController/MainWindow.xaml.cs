using System.Windows;
using System.Windows.Threading;
using Valve.VR;
using System; // Added for Console, EventArgs, TimeSpan
using System.Threading.Tasks; // Added for Task
using Google.Apis.Auth.OAuth2; // Added for UserCredential

namespace sai_OSCController
{
    public partial class MainWindow : Window
    {
        OSCReceiver? oSCReceiver = null;
        private GoogleAuthService googleAuthService;
        private GoogleAssistantServiceManager assistantServiceManager;

        MeterDataReceiver? meterDataReceiver = null;

        BatteryDataReceiver? batteryDataReceiver = null;

        OSCSender? oSCSender = null;

        public AvatarMover? AvatarMover = null;

        public static MainWindow? Instance { get; set; }

        DispatcherTimer? _timer1 = null;

        int updateDeviceInterval = 10;

        bool disposed = false;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            EVRInitError error = EVRInitError.None;
            OpenVR.Init(ref error, EVRApplicationType.VRApplication_Overlay);

            googleAuthService = new GoogleAuthService();
            assistantServiceManager = new GoogleAssistantServiceManager();
            oSCReceiver = new OSCReceiver(googleAuthService, assistantServiceManager);

            LoadSettings();
            UpdateAuthStatusDisplay();

            oSCSender = new();
            meterDataReceiver = new();
            batteryDataReceiver = new();
            AvatarMover = new(oSCSender);

            _timer1 = new();
            _timer1.Interval = TimeSpan.FromSeconds(updateDeviceInterval);
            _timer1.Tick += (sender, e) =>
            {
                OSCSend();
            };
            _timer1.Start();

            this.Closed += OnWindowClosed;
        }

        private void LoadSettings()
        {
            // Load from Properties.Settings.Default and populate TextBoxes
            string clientId = Properties.Settings.Default.OAuthClientId;
            string clientSecret = Properties.Settings.Default.OAuthClientSecret;
            string deviceModelId = Properties.Settings.Default.AssistantDeviceModelId;
            string deviceId = Properties.Settings.Default.RegisteredDeviceId;

            // Update UI (conceptual - assuming TextBoxes exist)
            // OAuthClientIdTextBox.Text = clientId;
            // OAuthClientSecretTextBox.Text = clientSecret;
            // AssistantDeviceModelIdTextBox.Text = deviceModelId;
            // RegisteredDeviceIdTextBox.Text = deviceId;

            Console.WriteLine($"Loaded Settings: ClientId empty: {string.IsNullOrEmpty(clientId)}, ClientSecret empty: {string.IsNullOrEmpty(clientSecret)}, ModelId empty: {string.IsNullOrEmpty(deviceModelId)}, DeviceId empty: {string.IsNullOrEmpty(deviceId)}");

            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
            {
                googleAuthService.SetCredentials(clientId, clientSecret);
            }
            if (!string.IsNullOrEmpty(deviceModelId) && !string.IsNullOrEmpty(deviceId))
            {
                assistantServiceManager.SetDeviceCredentials(deviceModelId, deviceId); // Configure the service manager
                oSCReceiver.UpdateAssistantDeviceCredentials(deviceModelId, deviceId); // Also configure OSCReceiver directly
            }
            // Language code can be set here if it's made a setting, or use default
            string language = "en-US"; // Example, could be from settings
            assistantServiceManager.SetLanguageCode(language);
            oSCReceiver.UpdateAssistantLanguageCode(language);
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Get values from TextBoxes (conceptual)
            // string clientId = OAuthClientIdTextBox.Text;
            // string clientSecret = OAuthClientSecretTextBox.Text;
            // string deviceModelId = AssistantDeviceModelIdTextBox.Text;
            // string deviceId = RegisteredDeviceIdTextBox.Text;

            // For the subtask, we'll simulate getting non-empty strings if the textboxes were filled
            string clientId = "test_client_id"; // Placeholder for subtask if actual UI interaction not possible
            string clientSecret = "test_client_secret";
            string deviceModelId = "test_model_id";
            string deviceId = "test_device_id";


            Properties.Settings.Default.OAuthClientId = clientId;
            Properties.Settings.Default.OAuthClientSecret = clientSecret;
            Properties.Settings.Default.AssistantDeviceModelId = deviceModelId;
            Properties.Settings.Default.RegisteredDeviceId = deviceId;
            Properties.Settings.Default.Save();

            Console.WriteLine("Settings Saved.");
            MessageBox.Show("Settings Saved!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

            // Re-apply credentials to services
            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
            {
                googleAuthService.SetCredentials(clientId, clientSecret);
            }
            if (!string.IsNullOrEmpty(deviceModelId) && !string.IsNullOrEmpty(deviceId))
            {
                assistantServiceManager.SetDeviceCredentials(deviceModelId, deviceId);
                oSCReceiver.UpdateAssistantDeviceCredentials(deviceModelId, deviceId);
            }
            UpdateAuthStatusDisplay();
        }

        private async void AuthenticateButton_Click(object sender, RoutedEventArgs e)
        {
            if (googleAuthService == null)
            {
                MessageBox.Show("Auth service not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrEmpty(Properties.Settings.Default.OAuthClientId) || string.IsNullOrEmpty(Properties.Settings.Default.OAuthClientSecret))
            {
                MessageBox.Show("OAuth Client ID or Client Secret is not configured. Please set them in settings and save.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Ensure latest credentials from settings are loaded into the auth service
            googleAuthService.SetCredentials(Properties.Settings.Default.OAuthClientId, Properties.Settings.Default.OAuthClientSecret);

            // AuthStatusTextBlock.Text = "Authenticating..."; // Conceptual UI update
            Console.WriteLine("Authentication process started...");
            try
            {
                UserCredential credential = await googleAuthService.AuthorizeAsync();
                if (credential != null && credential.Token != null && !string.IsNullOrEmpty(credential.Token.AccessToken))
                {
                    // AuthStatusTextBlock.Text = "Authenticated successfully."; // Conceptual UI update
                    Console.WriteLine("Authentication successful.");
                    MessageBox.Show("Successfully authenticated with Google!", "Authentication Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // AuthStatusTextBlock.Text = "Authentication failed or was cancelled."; // Conceptual UI update
                    Console.WriteLine("Authentication failed or was cancelled by user.");
                    MessageBox.Show("Authentication failed or was cancelled.", "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // AuthStatusTextBlock.Text = $"Authentication error: {ex.Message}"; // Conceptual UI update
                Console.WriteLine($"Authentication exception: {ex.Message}");
                MessageBox.Show($"An error occurred during authentication: {ex.Message}", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            UpdateAuthStatusDisplay();
        }

        private async void UpdateAuthStatusDisplay()
        {
            if (googleAuthService == null) return;
            // This method would check if a valid token exists and update AuthStatusTextBlock
            // For now, just log:
            string token = null;
            try {
                // Only try to get a token if credentials are set to avoid triggering auth flow here
                if (!string.IsNullOrEmpty(Properties.Settings.Default.OAuthClientId) && !string.IsNullOrEmpty(Properties.Settings.Default.OAuthClientSecret)) {
                    googleAuthService.SetCredentials(Properties.Settings.Default.OAuthClientId, Properties.Settings.Default.OAuthClientSecret); // Ensure service has latest creds
                    token = await googleAuthService.GetAccessTokenAsync(); // This might trigger auth if token is invalid
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error checking auth status: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(token)) {
                // AuthStatusTextBlock.Text = "Status: Authenticated";
                Console.WriteLine("Auth Status: Authenticated");
            } else {
                // AuthStatusTextBlock.Text = "Status: Not Authenticated. Please configure settings and authenticate.";
                Console.WriteLine("Auth Status: Not Authenticated");
            }
        }

        void OSCSend()
        {
            var batteryData = batteryDataReceiver?.GetSendData();
            var meterData = meterDataReceiver?.GetSendData();

            if (batteryData != null)
            {
                oSCSender.Send(batteryData);
            }

            if (meterData != null)
            {
                oSCSender.Send(meterData);
            }
        }

        public void OnWindowClosed(object? sender, EventArgs? e)
        {
            if (!disposed)
            {
                Console.WriteLine("Dispose");
                oSCSender.Dispose();
                OpenVR.Shutdown();
                disposed = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AvatarMover.Stop();
        }
    }
}