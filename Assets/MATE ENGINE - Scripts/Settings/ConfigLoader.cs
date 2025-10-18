using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace MateEngine.Settings
{
    [Serializable]
    public class GoogleConfig
    {
        public string clientId;
        public string clientSecret;
        public string[] scopes;
    }

    [Serializable]
    public class VoiceConfig
    {
        public string provider = "windows"; // windows | livekit | azure | custom
        public string sttProvider = "windows"; // windows | azure | custom
        public string ttsProvider = "windows";
    }

    [Serializable]
    public class OcrConfig
    {
        public string provider = "windows"; // windows | tesseract | custom
    }

    [Serializable]
    public class MateEngineConfig
    {
        public string environment = "dev"; // dev | prod
        public GoogleConfig google = new GoogleConfig();
        public VoiceConfig voice = new VoiceConfig();
        public OcrConfig ocr = new OcrConfig();
        public LiveKitConfig livekit = new LiveKitConfig();
    }

    [Serializable]
    public class LiveKitConfig
    {
        public string url;       // wss://<host>
        public string token;     // Access token (JWT) from your token service
        public string room;      // Optional room name to join
        public string identity;  // Optional user identity
        public string tokenEndpoint; // Optional: URL to fetch token dynamically
        public string apiKey;        // Optional: If your app requests tokens server-side
        public string apiSecret;     // Optional: Never commit; for dev only
    }

    public static class ConfigLoader
    {
        public static MateEngineConfig Current { get; private set; }
        private const string FileName = "mateengine.json";
        // Simple lock to avoid race conditions if multiple systems request a reload at the same time
        private static readonly object _sync = new object();

        #region Environment Variable Keys
        // Centralized names to avoid typos & enable future refactors
        private const string ENV_ENV = "MATEENGINE_ENV";
        private const string ENV_GOOGLE_CLIENT_ID = "MATEENGINE_GOOGLE_CLIENT_ID";
        private const string ENV_GOOGLE_CLIENT_SECRET = "MATEENGINE_GOOGLE_CLIENT_SECRET";
        private const string ENV_GOOGLE_SCOPES = "MATEENGINE_GOOGLE_SCOPES";
        private const string ENV_VOICE_PROVIDER = "MATEENGINE_VOICE_PROVIDER";
        private const string ENV_STT_PROVIDER = "MATEENGINE_STT_PROVIDER";
        private const string ENV_TTS_PROVIDER = "MATEENGINE_TTS_PROVIDER";
        private const string ENV_OCR_PROVIDER = "MATEENGINE_OCR_PROVIDER";
        private const string ENV_LK_URL = "MATEENGINE_LK_URL";
        private const string ENV_LK_TOKEN = "MATEENGINE_LK_TOKEN";
        private const string ENV_LK_ROOM = "MATEENGINE_LK_ROOM";
        private const string ENV_LK_IDENTITY = "MATEENGINE_LK_IDENTITY";
        private const string ENV_LK_TOKEN_ENDPOINT = "MATEENGINE_LK_TOKEN_ENDPOINT";
        private const string ENV_LK_API_KEY = "MATEENGINE_LK_API_KEY";
        private const string ENV_LK_API_SECRET = "MATEENGINE_LK_API_SECRET";
        #endregion

        public static void Load()
        {
            lock (_sync)
            {
                var loaded = LoadFromFilesWithMeta(out bool fromAppData, out bool fromStreaming);
                ApplyEnvOverrides(loaded);
                Normalize(loaded);
                Validate(loaded);
                Current = loaded;
                LogSummary(loaded, fromAppData, fromStreaming);
            }
        }

        /// <summary>
        /// Public convenience to explicitly reload configuration at runtime (e.g. after user edits AppData file)
        /// </summary>
        public static void Reload() => Load();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void AutoLoad()
        {
            try
            {
                Load();
            }
            catch (Exception e)
            {
                Debug.LogError("[ConfigLoader] Failed to load configuration at startup: " + e);
            }
        }

        private static MateEngineConfig LoadFromFilesWithMeta(out bool fromAppData, out bool fromStreaming)
        {
            fromAppData = false;
            fromStreaming = false;
            // AppData path (per-user)
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appDir = Path.Combine(appData, "MateEngine");
                string appConfigPath = Path.Combine(appDir, FileName);
                if (File.Exists(appConfigPath))
                {
                    string json = File.ReadAllText(appConfigPath, Encoding.UTF8);
                    fromAppData = true;
                    return JsonUtility.FromJson<MateEngineConfig>(json) ?? new MateEngineConfig();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ConfigLoader] Failed to read AppData config: " + e.Message);
            }

            // StreamingAssets fallback
            try
            {
                string streaming = Application.streamingAssetsPath;
                string path = Path.Combine(streaming, FileName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    fromStreaming = true;
                    return JsonUtility.FromJson<MateEngineConfig>(json) ?? new MateEngineConfig();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ConfigLoader] Failed to read StreamingAssets config: " + e.Message);
            }

            return new MateEngineConfig();
        }

        private static void ApplyEnvOverrides(MateEngineConfig cfg)
        {
            // Environment selection
            string env = Environment.GetEnvironmentVariable(ENV_ENV);
            if (!string.IsNullOrEmpty(env)) cfg.environment = env;

            // Google credentials
            string gClientId = Environment.GetEnvironmentVariable(ENV_GOOGLE_CLIENT_ID);
            if (!string.IsNullOrEmpty(gClientId)) cfg.google.clientId = gClientId;

            string gClientSecret = Environment.GetEnvironmentVariable(ENV_GOOGLE_CLIENT_SECRET);
            if (!string.IsNullOrEmpty(gClientSecret)) cfg.google.clientSecret = gClientSecret;

            string gScopes = Environment.GetEnvironmentVariable(ENV_GOOGLE_SCOPES);
            if (!string.IsNullOrEmpty(gScopes)) cfg.google.scopes = gScopes.Split(';');

            // Voice providers
            string voiceProvider = Environment.GetEnvironmentVariable(ENV_VOICE_PROVIDER);
            if (!string.IsNullOrEmpty(voiceProvider)) cfg.voice.provider = voiceProvider;
            string stt = Environment.GetEnvironmentVariable(ENV_STT_PROVIDER);
            if (!string.IsNullOrEmpty(stt)) cfg.voice.sttProvider = stt;

            string tts = Environment.GetEnvironmentVariable(ENV_TTS_PROVIDER);
            if (!string.IsNullOrEmpty(tts)) cfg.voice.ttsProvider = tts;

            // OCR provider
            string ocr = Environment.GetEnvironmentVariable(ENV_OCR_PROVIDER);
            if (!string.IsNullOrEmpty(ocr)) cfg.ocr.provider = ocr;

            // LiveKit
            string lkUrl = Environment.GetEnvironmentVariable(ENV_LK_URL);
            if (!string.IsNullOrEmpty(lkUrl)) cfg.livekit.url = lkUrl;
            string lkToken = Environment.GetEnvironmentVariable(ENV_LK_TOKEN);
            if (!string.IsNullOrEmpty(lkToken)) cfg.livekit.token = lkToken;
            string lkRoom = Environment.GetEnvironmentVariable(ENV_LK_ROOM);
            if (!string.IsNullOrEmpty(lkRoom)) cfg.livekit.room = lkRoom;
            string lkIdentity = Environment.GetEnvironmentVariable(ENV_LK_IDENTITY);
            if (!string.IsNullOrEmpty(lkIdentity)) cfg.livekit.identity = lkIdentity;
            string lkTokenEndpoint = Environment.GetEnvironmentVariable(ENV_LK_TOKEN_ENDPOINT);
            if (!string.IsNullOrEmpty(lkTokenEndpoint)) cfg.livekit.tokenEndpoint = lkTokenEndpoint;
            string lkApiKey = Environment.GetEnvironmentVariable(ENV_LK_API_KEY);
            if (!string.IsNullOrEmpty(lkApiKey)) cfg.livekit.apiKey = lkApiKey;
            string lkApiSecret = Environment.GetEnvironmentVariable(ENV_LK_API_SECRET);
            if (!string.IsNullOrEmpty(lkApiSecret)) cfg.livekit.apiSecret = lkApiSecret;
        }

        public static string GetGoogleClientId() => Current?.google?.clientId ?? string.Empty;
        public static string GetGoogleClientSecret() => Current?.google?.clientSecret ?? string.Empty;
        public static string[] GetGoogleScopes() => Current?.google?.scopes ?? Array.Empty<string>();

        // LiveKit convenience getters (non‑throwing)
        public static string GetLiveKitUrl() => Current?.livekit?.url ?? string.Empty;
        public static string GetLiveKitRoom() => Current?.livekit?.room ?? string.Empty;
        public static string GetLiveKitIdentity() => Current?.livekit?.identity ?? string.Empty;
        public static string GetLiveKitTokenEndpoint() => Current?.livekit?.tokenEndpoint ?? string.Empty;
        public static string GetLiveKitStaticToken() => Current?.livekit?.token ?? string.Empty;

        #region Normalization & Validation
        private static void Normalize(MateEngineConfig cfg)
        {
            if (cfg == null) return;
            cfg.environment = (cfg.environment ?? "dev").Trim();
            if (cfg.voice != null)
            {
                cfg.voice.provider = SafeLower(cfg.voice.provider);
                cfg.voice.sttProvider = SafeLower(cfg.voice.sttProvider);
                cfg.voice.ttsProvider = SafeLower(cfg.voice.ttsProvider);
            }
            if (cfg.ocr != null) cfg.ocr.provider = SafeLower(cfg.ocr.provider);
        }

        private static void Validate(MateEngineConfig cfg)
        {
            if (cfg?.voice?.provider == "livekit")
            {
                bool hasUrl = !string.IsNullOrWhiteSpace(cfg.livekit.url);
                bool hasTokenPath = !string.IsNullOrWhiteSpace(cfg.livekit.token) || !string.IsNullOrWhiteSpace(cfg.livekit.tokenEndpoint);
                if (!hasUrl || !hasTokenPath)
                {
                    Debug.LogWarning("[ConfigLoader] LiveKit provider selected but configuration incomplete: url=" + (hasUrl ? "ok" : "missing") + ", tokenOrEndpoint=" + (hasTokenPath ? "ok" : "missing"));
                }
            }
        }

        private static string SafeLower(string v) => string.IsNullOrWhiteSpace(v) ? string.Empty : v.Trim().ToLowerInvariant();
        #endregion

        #region Logging & Samples
        private static void LogSummary(MateEngineConfig cfg, bool fromAppData, bool fromStreaming)
        {
            string source = fromAppData ? "AppData" : (fromStreaming ? "StreamingAssets" : "defaults");
            string liveKitMode = cfg.voice.provider == "livekit" ? (string.IsNullOrEmpty(cfg.livekit.tokenEndpoint) ? "static-token" : "dynamic-token") : "n/a";
            Debug.Log($"[ConfigLoader] Loaded ({source}) env={cfg.environment} voice.provider={cfg.voice.provider} stt={cfg.voice.sttProvider} tts={cfg.voice.ttsProvider} ocr={cfg.ocr.provider} livekit.mode={liveKitMode}");
        }

        /// <summary>
        /// Writes a sample config file into the user's AppData folder if one does not already exist (or if overwrite=true).
        /// Secrets are left blank. Returns path or null on failure.
        /// </summary>
        public static string TryWriteSampleConfig(bool overwrite = false)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appDir = Path.Combine(appData, "MateEngine");
                if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
                string target = Path.Combine(appDir, FileName);
                if (File.Exists(target) && !overwrite) return target; // already there

                var sample = new MateEngineConfig
                {
                    environment = "dev",
                    google = new GoogleConfig { clientId = "", clientSecret = "", scopes = new[] { "openid", "profile" } },
                    voice = new VoiceConfig { provider = "windows", sttProvider = "windows", ttsProvider = "windows" },
                    ocr = new OcrConfig { provider = "windows" },
                    livekit = new LiveKitConfig { url = "wss://your-livekit-host", room = "mate-dev", identity = "local-user", tokenEndpoint = "http://127.0.0.1:8787/token" }
                };
                string json = JsonUtility.ToJson(sample, true);
                File.WriteAllText(target, json, Encoding.UTF8);
                Debug.Log("[ConfigLoader] Wrote sample config to: " + target);
                return target;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ConfigLoader] Failed to write sample config: " + e.Message);
                return null;
            }
        }
        #endregion
    }
}
