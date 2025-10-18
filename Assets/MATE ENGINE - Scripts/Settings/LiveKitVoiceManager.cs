using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

using MateEngine.Settings.LiveKitBridge;

namespace MateEngine.Settings
{
    [DisallowMultipleComponent]
    public class LiveKitVoiceManager : MonoBehaviour
    {
        public KeyCode toggleConnectKey = KeyCode.F9;
        public KeyCode pushToTalkKey = KeyCode.V;
        public KeyCode ttsTestKey = KeyCode.T;

    private bool wantsConnected = true; // auto-connect on start
        private bool isConnecting = false;
        private bool isConnected = false;
        private bool micPublishing = false;

        private string status = "idle";
    private ILiveKitAdapter adapter;
    private string resolvedUrl; // actual URL used (may come from token server response)

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            var cfg = ConfigLoader.Current?.livekit;
            if (cfg == null)
            {
                Debug.LogWarning("[LiveKitVoice] No LiveKit config found.");
                return;
            }

            // Choose adapter: SDK if available, else stub
#if LIVEKIT_SDK
            adapter = new LiveKitBridge.LiveKitAdapterSdk();
#else
            adapter = new LiveKitBridge.LiveKitAdapterStub();
            Debug.Log("[LiveKitVoice] LIVEKIT_SDK not defined. Using stub adapter.");
#endif

            // Try to connect (stub)
            if (wantsConnected)
            {
                StartCoroutine(ConnectRoutine());
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleConnectKey))
                wantsConnected = !wantsConnected;

            if (!isConnecting && wantsConnected && !isConnected)
                StartCoroutine(ConnectRoutine());

            if (!wantsConnected && isConnected)
                StartCoroutine(DisconnectRoutine());

            // Push-to-talk mic publish (requires consent)
            if (isConnected)
            {
                if (Input.GetKeyDown(pushToTalkKey) && ConsentManager.IsMicAllowed())
                {
                    SetMicPublishing(true);
                }
                else if (Input.GetKeyUp(pushToTalkKey))
                {
                    SetMicPublishing(false);
                }
            }

            if (Input.GetKeyDown(ttsTestKey))
            {
                Speak("LiveKit voice path ready.");
            }
        }

        private IEnumerator ConnectRoutine()
        {
            isConnecting = true;
            status = "connecting";

            var cfg = ConfigLoader.Current?.livekit;
            if (cfg == null)
            {
                status = "no-config";
                isConnecting = false;
                yield break;
            }

            // Fallbacks
            if (string.IsNullOrWhiteSpace(cfg.identity))
            {
                try { cfg.identity = System.Environment.UserName; }
                catch { cfg.identity = $"unity-{Random.Range(1000, 9999)}"; }
            }
            if (string.IsNullOrWhiteSpace(cfg.room))
            {
                cfg.room = "mate-engine-dev";
            }

            // Resolve token: prefer explicit token; otherwise tokenEndpoint if present
            string token = cfg.token;
            if (string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(cfg.tokenEndpoint))
            {
                yield return StartCoroutine(FetchToken(cfg.tokenEndpoint, (t) => token = t));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                status = "token-missing";
                isConnecting = false;
                yield break;
            }

            // Use adapter to connect (stub or SDK)
            resolvedUrl = string.IsNullOrWhiteSpace(resolvedUrl) ? cfg.url : resolvedUrl;
            yield return StartCoroutine(adapter.Connect(resolvedUrl, token));
            isConnected = adapter.IsConnected;
            isConnecting = false;
            status = adapter is LiveKitAdapterStub ? "connected (stub)" : (adapter.IsConnected ? "connected" : "disconnected");
            Debug.Log($"[LiveKitVoice] Connected via {(adapter is LiveKitAdapterStub ? "stub" : "sdk")} to {resolvedUrl} room={cfg.room} identity={cfg.identity}");
        }

        private IEnumerator DisconnectRoutine()
        {
            status = "disconnecting";
            if (adapter != null)
            {
                yield return StartCoroutine(adapter.Disconnect());
            }
            isConnected = false;
            micPublishing = false;
            status = "disconnected";
            Debug.Log("[LiveKitVoice] Disconnected");
        }

        private IEnumerator FetchToken(string endpoint, System.Action<string> onToken)
        {
            var cfg = ConfigLoader.Current?.livekit;
            string url = endpoint;
            if (cfg != null)
            {
                // Append identity and room if not already present
                var hasQuery = endpoint.Contains("?");
                string q = (hasQuery ? "&" : "?") +
                           (string.IsNullOrEmpty(cfg.identity) ? string.Empty : ("identity=" + UnityWebRequest.EscapeURL(cfg.identity))) +
                           (string.IsNullOrEmpty(cfg.identity) || string.IsNullOrEmpty(cfg.room) ? string.Empty : "&") +
                           (string.IsNullOrEmpty(cfg.room) ? string.Empty : ("room=" + UnityWebRequest.EscapeURL(cfg.room)));
                url = endpoint + q;
            }

            using (var req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("[LiveKitVoice] Token fetch failed: " + req.error);
                    onToken?.Invoke(null);
                }
                else
                {
                    // Expect raw token or JSON { token: "..." } or { accessToken: "..." } and optional { url: "wss://..." }
                    string body = req.downloadHandler.text;
                    string token = TryExtractToken(body);
                    // capture URL if provided in JSON
                    if (!string.IsNullOrEmpty(body) && body.TrimStart().StartsWith("{"))
                    {
                        string lkUrl = TryExtractJsonString(body, "\"url\"");
                        if (!string.IsNullOrWhiteSpace(lkUrl))
                        {
                            resolvedUrl = lkUrl;
                        }
                    }
                    onToken?.Invoke(token);
                }
            }
        }

        private string TryExtractToken(string body)
        {
            if (string.IsNullOrEmpty(body)) return null;
            // Quick heuristic: if it looks like JSON with a token field
            if (body.TrimStart().StartsWith("{"))
            {
                // Very small JSON parse without dependency
                var tok = TryExtractJsonString(body, "\"token\"") ?? TryExtractJsonString(body, "\"accessToken\"");
                if (!string.IsNullOrEmpty(tok)) return tok;
                return null;
            }
            return body; // assume raw token string
        }

        private string TryExtractJsonString(string json, string key)
        {
            int idx = json.IndexOf(key);
            if (idx >= 0)
            {
                int colon = json.IndexOf(":", idx);
                int firstQuote = json.IndexOf('"', colon + 1);
                int secondQuote = json.IndexOf('"', firstQuote + 1);
                if (firstQuote > 0 && secondQuote > firstQuote)
                {
                    return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                }
            }
            return null;
        }

        private void SetMicPublishing(bool publish)
        {
            if (publish && !ConsentManager.IsMicAllowed())
            {
                Debug.LogWarning("[LiveKitVoice] Mic publish blocked: no consent.");
                publish = false;
            }
            micPublishing = publish;
            if (adapter != null)
            {
                StartCoroutine(adapter.SetMicPublishing(publish));
            }
        }

        // Placeholder for TTS via LiveKit (e.g., server-side synthesis published to a track)
        public void Speak(string text)
        {
#if LIVEKIT_SDK
            // Send to a server agent or publish a data message to request TTS
#else
            Debug.Log($"[LiveKitVoice] Speak requested (stub): {text}");
#endif
        }

        private void OnGUI()
        {
            var cfg = ConfigLoader.Current?.livekit;
            GUILayout.BeginArea(new Rect(20, 20, 560, 180), GUI.skin.box);
            GUILayout.Label($"LiveKit: {(isConnected ? "CONNECTED" : isConnecting ? "CONNECTING" : wantsConnected ? "WILL CONNECT" : "DISCONNECTED")} | Status: {status}");
            if (cfg != null)
            {
                string urlShown = string.IsNullOrWhiteSpace(resolvedUrl) ? cfg.url : resolvedUrl;
                GUILayout.Label($"URL: {urlShown} | Room: {cfg.room ?? "(default)"} | Identity: {cfg.identity ?? "(auto)"}");
            }
            GUILayout.Label($"Adapter: {(adapter is LiveKitAdapterStub ? "stub" : "sdk")} | Remote participants: {adapter?.RemoteParticipantCount ?? 0}");
            GUILayout.Label($"Toggle Connect: {toggleConnectKey} | PTT (publish mic): hold {pushToTalkKey} | TTS Test: {ttsTestKey}");
            GUILayout.Label($"Consent: Mic {(ConsentManager.IsMicAllowed() ? "OK" : "BLOCKED")} | Publishing: {(micPublishing ? "YES" : "NO")}");
            GUILayout.EndArea();
        }
    }
}
