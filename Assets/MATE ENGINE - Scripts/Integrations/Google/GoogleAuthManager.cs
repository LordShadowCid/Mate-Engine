using System;
using System.Collections;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace MateEngine.Integrations.Google
{
    [DisallowMultipleComponent]
    public class GoogleAuthManager : MonoBehaviour
    {
        [Serializable]
        public class TokenData
        {
            public string access_token;
            public string refresh_token;
            public string token_type;
            public int expires_in;
            public string scope;
            public long obtained_unix; // when token was obtained

            [JsonIgnore]
            public DateTimeOffset ObtainedAt => DateTimeOffset.FromUnixTimeSeconds(obtained_unix);
        }

        [Serializable]
        private class DeviceCodeResponse
        {
            public string device_code;
            public string user_code;
            public string verification_url;
            public int expires_in;
            public int interval;
        }

        private const string DeviceCodeUrl = "https://oauth2.googleapis.com/device/code";
        private const string TokenUrl = "https://oauth2.googleapis.com/token";
        private const string TokenFileName = "google_tokens.json";

        private string FilePath => Path.Combine(Application.persistentDataPath, TokenFileName);
        public static GoogleAuthManager Instance { get; private set; }

        public TokenData Tokens { get; private set; }
        private DeviceCodeResponse currentDevice;
        private bool isAuthorizing;
        private string authStatus = "Idle";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadTokens();
        }

        public bool IsSignedIn() => Tokens != null && !string.IsNullOrEmpty(Tokens.access_token);

        public void SignOut()
        {
            Tokens = null;
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
            authStatus = "Signed out";
        }

        public void BeginDeviceCodeFlow()
        {
            if (!MateEngine.Settings.ConsentManager.IsGoogleAllowed())
            {
                authStatus = "Google access not consented";
                return;
            }
            if (isAuthorizing) return;
            StartCoroutine(DeviceCodeCoroutine());
        }

        private IEnumerator DeviceCodeCoroutine()
        {
            isAuthorizing = true;
            authStatus = "Requesting device code";

            var clientId = MateEngine.Settings.ConfigLoader.GetGoogleClientId();
            var clientSecret = MateEngine.Settings.ConfigLoader.GetGoogleClientSecret();
            var scopes = MateEngine.Settings.ConfigLoader.GetGoogleScopes();
            string scope = scopes != null && scopes.Length > 0 ? string.Join(" ", scopes) : "https://www.googleapis.com/auth/gmail.readonly";

            WWWForm form = new WWWForm();
            form.AddField("client_id", clientId);
            form.AddField("scope", scope);

            using (var req = UnityWebRequest.Post(DeviceCodeUrl, form))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    authStatus = "Device code failed: " + req.error;
                    isAuthorizing = false;
                    yield break;
                }
                currentDevice = JsonConvert.DeserializeObject<DeviceCodeResponse>(req.downloadHandler.text);
            }

            if (currentDevice == null || string.IsNullOrEmpty(currentDevice.device_code))
            {
                authStatus = "Invalid device code response";
                isAuthorizing = false;
                yield break;
            }

            authStatus = $"Go to {currentDevice.verification_url} and enter code: {currentDevice.user_code}";

            // Poll token endpoint
            float interval = Mathf.Max(5, currentDevice.interval);
            float deadline = Time.realtimeSinceStartup + currentDevice.expires_in;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForSeconds(interval);

                WWWForm tokenForm = new WWWForm();
                tokenForm.AddField("client_id", clientId);
                if (!string.IsNullOrEmpty(clientSecret)) tokenForm.AddField("client_secret", clientSecret);
                tokenForm.AddField("device_code", currentDevice.device_code);
                tokenForm.AddField("grant_type", "urn:ietf:params:oauth:grant-type:device_code");

                using (var tokenReq = UnityWebRequest.Post(TokenUrl, tokenForm))
                {
                    yield return tokenReq.SendWebRequest();
                    if (tokenReq.result != UnityWebRequest.Result.Success)
                    {
                        // Look for slow_down/authorization_pending
                        if (!string.IsNullOrEmpty(tokenReq.downloadHandler.text) && tokenReq.downloadHandler.text.Contains("authorization_pending"))
                        {
                            authStatus = "Waiting for authorization...";
                            continue;
                        }
                        else if (!string.IsNullOrEmpty(tokenReq.downloadHandler.text) && tokenReq.downloadHandler.text.Contains("slow_down"))
                        {
                            interval += 5f; // backoff
                            continue;
                        }
                        else
                        {
                            authStatus = "Token request error: " + tokenReq.error;
                            break;
                        }
                    }

                    var token = JsonConvert.DeserializeObject<TokenData>(tokenReq.downloadHandler.text);
                    if (!string.IsNullOrEmpty(token?.access_token))
                    {
                        token.obtained_unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        Tokens = token;
                        SaveTokens();
                        authStatus = "Authorized";
                        isAuthorizing = false;
                        yield break;
                    }
                    else
                    {
                        // Might still be pending
                        authStatus = "Awaiting approval...";
                    }
                }
            }

            isAuthorizing = false;
            authStatus = "Authorization timed out";
        }

        public IEnumerator EnsureAccessToken(Action<string> onReady)
        {
            if (Tokens == null)
            {
                onReady?.Invoke(null);
                yield break;
            }

            var expiry = DateTimeOffset.FromUnixTimeSeconds(Tokens.obtained_unix).AddSeconds(Tokens.expires_in - 30);
            if (DateTimeOffset.UtcNow < expiry)
            {
                onReady?.Invoke(Tokens.access_token);
                yield break;
            }

            // Refresh
            var clientId = MateEngine.Settings.ConfigLoader.GetGoogleClientId();
            var clientSecret = MateEngine.Settings.ConfigLoader.GetGoogleClientSecret();

            WWWForm form = new WWWForm();
            form.AddField("client_id", clientId);
            if (!string.IsNullOrEmpty(clientSecret)) form.AddField("client_secret", clientSecret);
            form.AddField("refresh_token", Tokens.refresh_token);
            form.AddField("grant_type", "refresh_token");

            using (var req = UnityWebRequest.Post(TokenUrl, form))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("[GoogleAuth] Refresh failed: " + req.error);
                    onReady?.Invoke(null);
                    yield break;
                }
                var token = JsonConvert.DeserializeObject<TokenData>(req.downloadHandler.text);
                if (!string.IsNullOrEmpty(token?.access_token))
                {
                    // Keep existing refresh_token if missing in response
                    if (string.IsNullOrEmpty(token.refresh_token) && !string.IsNullOrEmpty(Tokens.refresh_token))
                        token.refresh_token = Tokens.refresh_token;
                    token.obtained_unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    Tokens = token;
                    SaveTokens();
                    onReady?.Invoke(Tokens.access_token);
                }
                else
                {
                    onReady?.Invoke(null);
                }
            }
        }

        private void SaveTokens()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var json = JsonConvert.SerializeObject(Tokens, Formatting.Indented);
                File.WriteAllText(FilePath, json, Encoding.UTF8);
                Debug.Log("[GoogleAuth] Tokens saved");
            }
            catch (Exception e)
            {
                Debug.LogError("[GoogleAuth] Save failed: " + e.Message);
            }
        }

        private void LoadTokens()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath, Encoding.UTF8);
                    Tokens = JsonConvert.DeserializeObject<TokenData>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GoogleAuth] Load failed: " + e.Message);
            }
        }

        private void OnGUI()
        {
            // Minimal overlay w/ consent gating
            GUILayout.BeginArea(new Rect(Screen.width - 360, 20, 340, 200), GUI.skin.box);
            GUILayout.Label("Google Auth");
            GUILayout.Label("Consent: " + (MateEngine.Settings.ConsentManager.IsGoogleAllowed() ? "Allowed" : "Blocked"));
            GUILayout.Label("Status: " + authStatus);
            if (!IsSignedIn())
            {
                GUI.enabled = MateEngine.Settings.ConsentManager.IsGoogleAllowed();
                if (GUILayout.Button("Sign In (Device Code)")) BeginDeviceCodeFlow();
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label("Signed in");
                if (GUILayout.Button("Sign Out")) SignOut();
            }
            GUILayout.EndArea();
        }
    }
}
