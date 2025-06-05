using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows; // For MessageBox - consider abstracting for testability if time permits

namespace sai_OSCController
{
    public class GoogleAuthService
    {
        private const string UserCredentialsFileName = "google_assistant_credentials.json";
        private string clientId = "";
        private string clientSecret = "";
        private readonly string[] scopes = { "https://www.googleapis.com/auth/assistant-sdk-prototype" };
        private string redirectUri;

        // Data Protection API scope
        private static readonly DataProtectionScope ProtectionScope = DataProtectionScope.CurrentUser;

        public GoogleAuthService()
        {
            redirectUri = $"http://localhost:{GetRandomUnusedPort()}/";
        }

        public void SetCredentials(string oauthClientId, string oauthClientSecret)
        {
            this.clientId = oauthClientId;
            this.clientSecret = oauthClientSecret;
            Console.WriteLine("OAuth Client ID and Secret have been set in GoogleAuthService.");
        }

        public async Task<UserCredential> AuthorizeAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Console.WriteLine("OAuth Client ID or Client Secret is not set. Cannot authorize.");
                MessageBox.Show("OAuth Client ID or Client Secret is not configured. Please configure them in settings.", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            var clientSecrets = new ClientSecrets
            {
                ClientId = this.clientId,
                ClientSecret = this.clientSecret
            };

            try
            {
                // Using a custom IDataStore for secure storage of the refresh token
                var dataStore = new ProtectedDataStore(GetUserDataPath());

                var codeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets,
                    Scopes = scopes,
                    DataStore = dataStore,
                });

                // The UserCredential object will attempt to load existing tokens from the IDataStore.
                // If not found, or if they are expired and no refresh token is present, it will initiate the auth flow.
                var userCredential = await new AuthorizationCodeInstalledApp(codeFlow, new LocalServerCodeReceiver(redirectUri))
                    .AuthorizeAsync("user", cancellationToken);

                if (userCredential.Token != null)
                {
                    Console.WriteLine("Successfully authorized and obtained token.");
                    if (userCredential.Token.RefreshToken != null)
                    {
                        Console.WriteLine("Refresh token is present.");
                        // The token is already saved by ProtectedDataStore at this point if new/refreshed.
                    }
                }
                else
                {
                    Console.WriteLine("Authorization did not result in a token.");
                }
                return userCredential;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OAuth authorization error: {ex.Message}");
                MessageBox.Show($"An error occurred during Google authentication: {ex.Message}\n\nPlease ensure you have internet connectivity and that your OAuth credentials are correct. Check the console for more details.", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Console.WriteLine("OAuth Client ID or Client Secret is not set. Cannot get access token.");
                return null;
            }

            UserCredential credential = await AuthorizeAsync(cancellationToken); // This will load stored tokens or re-authorize
            if (credential?.Token != null)
            {
                if (credential.Token.IsExpired(credential.Flow.Clock))
                {
                    Console.WriteLine("Access token expired, attempting to refresh.");
                    bool refreshed = await credential.RefreshTokenAsync(CancellationToken.None);
                    if (refreshed)
                    {
                        Console.WriteLine("Access token refreshed successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Failed to refresh access token. User might need to re-authorize.");
                        // May need to explicitly trigger re-authorization here if refresh fails persistently.
                        // For now, returning potentially stale or null token.
                         return null;
                    }
                }
                return credential.Token.AccessToken;
            }
            return null;
        }

        public async Task RevokeTokensAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Console.WriteLine("OAuth Client ID or Client Secret is not set. Cannot revoke tokens.");
                return;
            }

            UserCredential credential = await AuthorizeAsync(cancellationToken); // Load existing credential
            if (credential?.Token != null)
            {
                try
                {
                    if (credential.Token.RefreshToken != null)
                    {
                         await credential.RevokeTokenAsync(CancellationToken.None); // Revoke both access and refresh token
                         Console.WriteLine("Tokens revoked successfully.");
                    }
                    else // Only access token to revoke (or already revoked)
                    {
                        // The library doesn't have a specific method for just access token if no refresh token.
                        // Clearing from datastore is the main action.
                         Console.WriteLine("No refresh token found, clearing local token store.");
                    }
                    // Clear the stored tokens after revocation
                    var dataStore = new ProtectedDataStore(GetUserDataPath());
                    await dataStore.DeleteAsync<TokenResponse>("user");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error revoking tokens: {ex.Message}");
                }
            }
        }

        private static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string GetUserDataPath()
        {
            // Store credentials in a subfolder of LocalApplicationData
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataPath, "sai_OSCController", "Auth");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            return appFolder;
        }
    }

    // Custom IDataStore using Windows Data Protection API for secure refresh token storage
    public class ProtectedDataStore : IDataStore
    {
        private readonly string folderPath;

        public ProtectedDataStore(string folder)
        {
            folderPath = folder;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        public Task StoreAsync<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(value);
            var encryptedData = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(serialized),
                null, // Optional entropy
                DataProtectionScope.CurrentUser);

            File.WriteAllBytes(Path.Combine(folderPath, key), encryptedData);
            return Task.CompletedTask;
        }

        public Task DeleteAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            var filePath = Path.Combine(folderPath, key);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }

        public Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            var filePath = Path.Combine(folderPath, key);
            if (!File.Exists(filePath))
            {
                return Task.FromResult(default(T));
            }

            try
            {
                var encryptedData = File.ReadAllBytes(filePath);
                var decryptedData = ProtectedData.Unprotect(
                    encryptedData,
                    null, // Optional entropy
                    DataProtectionScope.CurrentUser);

                var serialized = Encoding.UTF8.GetString(decryptedData);
                return Task.FromResult(Newtonsoft.Json.JsonConvert.DeserializeObject<T>(serialized));
            }
            catch (CryptographicException) // Could happen if file is corrupted or moved between users/machines
            {
                Console.WriteLine($"Failed to decrypt token data for key {key}. Possible data corruption or access issue.");
                File.Delete(filePath); // Delete corrupted/unusable token data
                return Task.FromResult(default(T));
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"Error getting token data for key {key}: {ex.Message}");
                 return Task.FromResult(default(T));
            }
        }

        public Task ClearAsync()
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
                Directory.CreateDirectory(folderPath); // Recreate for future use
            }
            return Task.CompletedTask;
        }
    }
}
