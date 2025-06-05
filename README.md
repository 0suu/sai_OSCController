# sai_OSCController - VRChat to Google Assistant Integration

This application allows you to send commands from VRChat via OSC to Google Assistant, enabling control of home appliances or other actions recognized by your Google Assistant. This integration uses the Google Assistant Service API directly.

**IMPORTANT: The Google Assistant SDK (which this application uses) is intended for experimental and non-commercial use ONLY.**

## Setup Instructions

Setting up this integration involves several steps across Google Cloud Platform and this application. Please follow them carefully.

### 1. Google Cloud Platform Project Setup

a.  **Create or Select a GCP Project:**
    *   Go to the [Google Cloud Console](https://console.cloud.google.com/).
    *   Create a new project or select an existing one.

b.  **Enable the Google Assistant API:**
    *   In your GCP project, navigate to "APIs & Services" > "Library".
    *   Search for "Google Assistant API" (it might be listed as "Google Assistant Service API" or similar). Enable it.
    *   If you are asked to enable billing, you may need to do so, though the API itself has a free tier for usage. Be aware of Google Cloud pricing policies.

c.  **Configure OAuth 2.0 Consent Screen:**
    *   Go to "APIs & Services" > "OAuth consent screen".
    *   Choose "External" for User Type (unless you have a Google Workspace organization). Click "Create".
    *   Fill in the required fields:
        *   **App name:** (e.g., "VRChat OSC Assistant Controller")
        *   **User support email:** Your email address.
        *   **Developer contact information:** Your email address.
    *   Click "Save and Continue" through Scopes and Test Users. You don't need to add specific scopes here; the application will request the necessary scope.
    *   On the "Summary" page, click "Back to Dashboard". You might need to "Publish App" later if it stays in "testing" mode for too long, but for personal use, "testing" mode with your own account as a test user is often sufficient.

d.  **Create OAuth 2.0 Client ID Credentials:**
    *   Go to "APIs & Services" > "Credentials".
    *   Click "+ CREATE CREDENTIALS" > "OAuth client ID".
    *   Select "Desktop app" for Application type.
    *   Give it a name (e.g., "VRChat OSC Assistant Desktop Client").
    *   Click "Create".
    *   A dialog will show your **Client ID** and **Client Secret**. **Copy these down immediately and store them securely.** You will need them for the application's settings. You can also download the credentials as a JSON file by clicking the download icon next to the client ID in the list later (this JSON is different from the one used for device registration).

### 2. Device Registration

This step registers your application instance as a "device" with Google Assistant.

a.  **Install `googlesamples-assistant-devicetool`:**
    *   This is a Python command-line tool. Ensure you have Python installed.
    *   Install the tool: `pip install googlesamples-assistant-devicetool`
    *   Ensure it's in your system's PATH or use `python -m googlesamples.assistant.devicetool ...`

b.  **Register a Device Model:**
    *   Run the following command, replacing `<YOUR_PROJECT_ID>` with your Google Cloud Project ID (from step 1a), and give your product a name and manufacturer name.
        ```bash
        googlesamples-assistant-devicetool register-model --project <YOUR_PROJECT_ID> --product-name "VRChatOSCAssistant" --manufacturer-name "YourName" --device-type LIGHT
        ```
        *   `(Device type can be LIGHT, SWITCH, etc. It's mostly representational for this SDK usage but choose one.)`
    *   This command will output a list of device models. Note the **Model ID** (e.g., `<YOUR_PROJECT_ID>-vrchatoscassistant`). You'll need this for the application settings.

c.  **Download Client Secret JSON for Device Registration:**
    *   Go back to your GCP project's "Credentials" page.
    *   You should see an OAuth 2.0 Client ID of type "Other" or "Device" created by the `register-model` command (it might be named after your project ID or model).
    *   Download the JSON credentials file for **this specific client ID**. It will be named something like `client_secret_....json`. This is the file the `register-device` tool (and our app's OAuth flow initially expects via the Python tool, though our app will use the Client ID/Secret directly from settings). **This file is important.**
        *   **Crucial Clarification**: The `googlesamples-assistant-devicetool` often creates its own OAuth client ID of type "Other". The `client_secret_....json` file downloaded from *this specific entry* is what the tool (and the Python samples) uses for authentication during device registration and for the `google-oauthlib-tool`.
        *   For our WPF application, we created a "Desktop app" OAuth client ID in step 1d. **It's the Client ID and Client Secret from the "Desktop app" credential that you should use in the `sai_OSCController` application settings.** The `client_secret_....json` from the device model registration is primarily for the `googlesamples-assistant-devicetool` itself and the Python `google-oauthlib-tool`.

d.  **Register a Device Instance (Optional but Recommended for Clarity):**
    *   You can also register a specific device instance using the tool, which helps manage multiple "devices" if needed.
    *   Choose a **Device ID (Instance ID)** yourself (e.g., "vrchat-osc-device1"). This is the ID you will put into the application settings as "Registered Device ID".
        ```bash
        googlesamples-assistant-devicetool register-device --project <YOUR_PROJECT_ID> --model <YOUR_MODEL_ID> --client-type OTHER --device <YOUR_CHOSEN_DEVICE_ID>
        ```
    *   While the tool can do this, for our app, the key is that the user defines a `RegisteredDeviceId` (instance ID) and an `AssistantDeviceModelId` in the settings, and these are used in API calls.

### 3. Application Configuration (`sai_OSCController`)

a.  **Launch `sai_OSCController.exe`.**

b.  **Enter Settings:**
    *   The application will require you to input:
        *   **OAuth Client ID:** (From step 1d - your "Desktop app" credential)
        *   **OAuth Client Secret:** (From step 1d - your "Desktop app" credential)
        *   **Assistant Device Model ID:** (From step 2b - e.g., `<YOUR_PROJECT_ID>-vrchatoscassistant`)
        *   **Registered Device ID (Instance ID):** (A unique ID you define for this application instance, e.g., "my-vrchat-osc-controller". This is used as `device_id` in API calls).
    *   These settings are typically entered via UI elements in the application (if implemented) or by directly editing the `user.config` file (see Troubleshooting section). Click "Save Settings" in the UI if available.

c.  **Authenticate with Google:**
    *   Click the "Authenticate with Google" (or similar) button in the application.
    *   Your web browser will open to a Google consent page.
    *   Log in with the Google account that has access to your Google Home devices and the GCP project.
    *   Grant the permissions requested by the application (it will request the `https://www.googleapis.com/auth/assistant-sdk-prototype` scope).
    *   After granting permission, you should be redirected to a local address (e.g., `http://localhost:port/`). The application will capture this redirect and complete the authentication.
    *   The application should indicate if authentication was successful.

### 4. VRChat OSC Setup

a.  **Enable OSC in VRChat.**
b.  **Configure an OSC message:**
    *   The application listens for messages at the address: `/avatar/parameters/GoogleAssistantQuery`
    *   The **first argument** of this OSC message must be a **string** containing the text command you want to send to Google Assistant (e.g., "turn on the living room lights").

### How It Works

1.  VRChat sends an OSC message (e.g., `/avatar/parameters/GoogleAssistantQuery` "turn on the lights") to `sai_OSCController`.
2.  `sai_OSCController` receives this message.
3.  It uses the authenticated user's credentials (OAuth 2.0 token) and the registered device identifiers to send the text command ("turn on the lights") directly to the Google Assistant Service API.
4.  Google Assistant processes this command based on your Google account's settings, including linked smart home devices in your Google Home app.
5.  The text response from Google Assistant (e.g., "Okay, turning on the living room lights") is logged in the `sai_OSCController` console.

### Home Appliance Control

*   For home appliance control to work, you must have already configured your smart home devices within your Google Home app and ensure they are controllable by Google Assistant using voice commands from a Google Home speaker or the Assistant app on your phone.
*   This application sends the text query *as if you spoke it to your Assistant*. The Assistant then handles it based on your existing device setup.

### Troubleshooting

*   **Authentication Errors (`invalid_client`, `redirect_uri_mismatch`, etc.):**
    *   Double-check that the Client ID and Client Secret in the application settings exactly match the "Desktop app" OAuth 2.0 credentials from your GCP project.
    *   Ensure the OAuth consent screen is configured.
*   **No Response or Errors from Assistant:**
    *   Verify the Assistant Device Model ID and Registered Device ID (Instance ID) are correctly entered in the application settings.
    *   Check the `sai_OSCController` console for detailed error messages from the Google Assistant Service API.
    *   Ensure the Google Assistant API is enabled in your GCP project.
    *   Test if the command works when spoken directly to a Google Home device or the Assistant app on your phone.
*   **OSC Messages Not Working:**
    *   Confirm the OSC address and argument format in VRChat.
    *   Ensure VRChat OSC is enabled and sending to the correct IP/port (this app listens on UDP port 9001 by default).
*   **Editing `user.config` Manually (Advanced):**
    *   If UI for settings is not yet fully implemented or you need to reset/check settings:
    *   The `user.config` file is typically located in: `C:\Users\<YourUserName>\AppData\Local\sai_OSCController\sai_OSCController.exe_Url_\<Version>\user.config`.
    *   You can edit the XML values for `OAuthClientId`, `OAuthClientSecret`, `AssistantDeviceModelId`, and `RegisteredDeviceId`.
    *   To clear stored OAuth tokens (if you suspect they are corrupt), delete the `Auth` subfolder within `C:\Users\<YourUserName>\AppData\Local\sai_OSCController\`. The application will then require you to re-authenticate.
