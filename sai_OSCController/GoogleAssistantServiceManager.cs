using Google.Assistant.Embedded.V1Alpha2; // Assuming this is the namespace for generated gRPC code
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Threading.Tasks;
using System.Linq; // For FirstOrDefault
using System.Text; // For StringBuilder

namespace sai_OSCController
{
    public class GoogleAssistantServiceManager
    {
        private const string AssistantApiEndpoint = "embeddedassistant.googleapis.com";
        private EmbeddedAssistant.EmbeddedAssistantClient client;
        private string languageCode = "en-US"; // Default, can be made configurable
        private string deviceModelId = ""; // User needs to set this - obtained during device registration
        private string deviceInstanceId = ""; // User needs to set this - obtained/chosen during device registration


        public GoogleAssistantServiceManager()
        {
            // Client is created when needed with the token
        }

        public void SetDeviceCredentials(string modelId, string instanceId)
        {
            this.deviceModelId = modelId;
            this.deviceInstanceId = instanceId;
            Console.WriteLine($"Assistant Device Model ID: {modelId}, Device Instance ID: {instanceId}");
        }

        public void SetLanguageCode(string langCode)
        {
            this.languageCode = langCode;
            Console.WriteLine($"Assistant Language Code set to: {langCode}");
        }

        private void EnsureClientCreated(string accessToken)
        {
            if (client == null)
            {
                var channel = GrpcChannel.ForAddress($"https://{AssistantApiEndpoint}");
                var callCredentials = CallCredentials.FromInterceptor(async (context, metadata) =>
                {
                    metadata.Add("Authorization", $"Bearer {accessToken}");
                });

                var channelWithCreds = channel.CreateCallInvoker(callCredentials);
                client = new EmbeddedAssistant.EmbeddedAssistantClient(channelWithCreds);
                Console.WriteLine("gRPC client for Google Assistant Service created.");
            }
        }

        public async Task<string> SendTextQueryAsync(string textQuery, string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("Access token is missing. Cannot send query.");
                return "Error: Missing access token.";
            }
            if (string.IsNullOrEmpty(deviceModelId) || string.IsNullOrEmpty(deviceInstanceId))
            {
                Console.WriteLine("Device Model ID or Device Instance ID is not set. Cannot send query.");
                return "Error: Device Model ID or Instance ID not set.";
            }

            EnsureClientCreated(accessToken);

            Console.WriteLine($"Sending text query to Google Assistant: '{textQuery}'");

            using (var call = client.Assist())
            {
                var assistConfig = new AssistConfig
                {
                    TextQuery = textQuery, // Specify text query
                    AudioOutConfig = new AudioOutConfig // Request audio output config even if we primarily want text
                    {
                        Encoding = AudioOutConfig.Types.AudioEncoding.Linear16,
                        SampleRateHertz = 16000,
                        VolumePercentage = 0 // We don't want audio playback in this app, but field is often required
                    },
                    DialogStateIn = new DialogStateIn
                    {
                        LanguageCode = this.languageCode,
                        // No conversation state needed for single-turn queries
                    },
                    DeviceConfig = new DeviceConfig
                    {
                        DeviceId = this.deviceInstanceId, // Use Device Instance ID
                        DeviceModelId = this.deviceModelId  // Use Device Model ID
                    }
                };

                // For text query, we only send one request with the config.
                // No audio input is streamed.
                await call.RequestStream.WriteAsync(new AssistRequest { Config = assistConfig });
                await call.RequestStream.CompleteAsync();

                StringBuilder responseBuilder = new StringBuilder();
                string lastSupplementalDisplayText = null;

                try
                {
                    await foreach (var response in call.ResponseStream.ReadAllAsync())
                    {
                        if (response.EventType == AssistResponse.Types.EventType.EndOfUtterance)
                        {
                            Console.WriteLine("Google Assistant: End of user utterance detected by server.");
                        }

                        if (!string.IsNullOrEmpty(response.DialogStateOut?.SupplementalDisplayText))
                        {
                            lastSupplementalDisplayText = response.DialogStateOut.SupplementalDisplayText;
                            Console.WriteLine($"Assistant Display Text: {lastSupplementalDisplayText}");
                        }

                        // Log other potentially useful info
                        if (response.SpeechResults.Any())
                        {
                            var result = response.SpeechResults.FirstOrDefault(r => r.Stability == 1.0);
                            if (result != null) {
                                Console.WriteLine($"Assistant Speech Result (stable): {result.Transcript}");
                                // If supplemental_display_text is empty, this might be the primary text response
                                if(string.IsNullOrEmpty(lastSupplementalDisplayText)) {
                                    responseBuilder.AppendLine(result.Transcript);
                                }
                            }
                        }
                        // You might also inspect response.DeviceAction for smart home commands,
                        // but handling that is beyond simple text response.
                    }
                }
                catch (RpcException ex)
                {
                    Console.WriteLine($"gRPC Exception during Assist call: {ex.Status}");
                    return $"Error: {ex.Status.Detail}";
                }
                catch (Exception ex)
                {
                     Console.WriteLine($"Exception during Assist call: {ex.Message}");
                    return $"Error: {ex.Message}";
                }

                // Prefer supplemental_display_text if available, otherwise use concatenated transcripts
                if (!string.IsNullOrEmpty(lastSupplementalDisplayText)) {
                    return lastSupplementalDisplayText;
                }
                return responseBuilder.Length > 0 ? responseBuilder.ToString().Trim() : "No text response received.";
            }
        }
    }
}

// Add a placeholder for the generated gRPC code namespace if it's different
// For example, if the .proto package is `google.assistant.embedded.v1alpha2`
// the C# namespace might be `Google.Assistant.Embedded.V1alpha2`
namespace Google.Assistant.Embedded.V1Alpha2
{
    // This is just a placeholder to allow the above code to compile conceptually
    // The actual generated classes will be created by Grpc.Tools
    public static partial class EmbeddedAssistant { public partial class EmbeddedAssistantClient { public virtual Grpc.Core.AsyncDuplexStreamingCall<AssistRequest, AssistResponse> Assist(Grpc.Core.Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken)) { throw new NotImplementedException(); } } }
    public partial class AssistRequest { public AssistConfig Config { get; set; } }
    public partial class AssistResponse { public DialogStateOut DialogStateOut { get; set; } public System.Collections.Generic.IEnumerable<SpeechRecognitionResult> SpeechResults { get; set; } public static class Types { public enum EventType { EndOfUtterance } } public Types.EventType EventType {get; set;} }
    public partial class AssistConfig { public string TextQuery { get; set; } public AudioOutConfig AudioOutConfig { get; set; } public DialogStateIn DialogStateIn { get; set; } public DeviceConfig DeviceConfig { get; set; } }
    public partial class AudioOutConfig { public enum Types { public enum AudioEncoding { Linear16 } } public Types.AudioEncoding Encoding { get; set; } public int SampleRateHertz { get; set; } public int VolumePercentage { get; set; } }
    public partial class DialogStateIn { public string LanguageCode { get; set; } }
    public partial class DeviceConfig { public string DeviceId { get; set; } public string DeviceModelId { get; set; } }
    public partial class DialogStateOut { public string SupplementalDisplayText { get; set; } }
    public partial class SpeechRecognitionResult { public string Transcript { get; set; } public float Stability { get; set; } }
}
