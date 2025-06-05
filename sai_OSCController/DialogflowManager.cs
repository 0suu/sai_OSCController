using Google.Cloud.Dialogflow.V2;
using System;
using System.Threading.Tasks;

namespace sai_OSCController
{
    public class DialogflowManager
    {
        private SessionsClient sessionsClient;
        private string currentProjectId; // To store the project ID

        public DialogflowManager()
        {
            // Initialize the SessionsClient.
            // This will use the GOOGLE_APPLICATION_CREDENTIALS environment variable automatically.
            try
            {
                sessionsClient = SessionsClient.Create();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating SessionsClient: {ex.Message}. Ensure GOOGLE_APPLICATION_CREDENTIALS is set correctly.");
                // Potentially re-throw or handle more gracefully depending on application requirements
                throw;
            }
        }

        // Method to update Project ID if it's configurable later
        public void SetProjectId(string projectId)
        {
            currentProjectId = projectId;
            Console.WriteLine($"Dialogflow Project ID set to: {projectId}");
        }

        public async Task<DetectIntentResponse> DetectIntentAsync(string sessionId, string text, string languageCode = "en-US")
        {
            if (string.IsNullOrEmpty(currentProjectId))
            {
                Console.WriteLine("Error: Dialogflow Project ID is not set. Call SetProjectId first.");
                return null; // Or throw an exception
            }

            if (sessionsClient == null)
            {
                 Console.WriteLine("Error: SessionsClient is not initialized.");
                 return null; // Or throw an exception
            }

            var sessionName = SessionName.FromProjectSession(currentProjectId, sessionId);
            var queryInput = new QueryInput
            {
                Text = new TextInput
                {
                    Text = text,
                    LanguageCode = languageCode
                }
            };

            try
            {
                Console.WriteLine($"Sending text to Dialogflow: '{text}' for session: {sessionId} in project: {currentProjectId}");
                DetectIntentResponse response = await sessionsClient.DetectIntentAsync(sessionName, queryInput);
                Console.WriteLine($"Dialogflow response: {response.QueryResult.FulfillmentText}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling Dialogflow DetectIntentAsync: {ex.Message}");
                return null; // Or throw an exception
            }
        }

        // Overload for convenience if project ID is passed directly, though SetProjectId is preferred for persistent setting
        public async Task<DetectIntentResponse> DetectIntentAsync(string projectId, string sessionId, string text, string languageCode = "en-US")
        {
            SetProjectId(projectId); // Set it for this call if not already set or to override
            return await DetectIntentAsync(sessionId, text, languageCode);
        }
    }
}
