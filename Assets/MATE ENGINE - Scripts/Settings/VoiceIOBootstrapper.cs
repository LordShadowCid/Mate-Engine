using UnityEngine;

namespace MateEngine.Settings
{
    public static class VoiceIOBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureVoiceIO()
        {
            var provider = ConfigLoader.Current?.voice?.provider ?? "windows";
            if (provider.Equals("livekit", System.StringComparison.OrdinalIgnoreCase))
            {
                if (Object.FindFirstObjectByType<LiveKitVoiceManager>() == null)
                {
                    var go = new GameObject("LiveKitVoiceManager");
                    Object.DontDestroyOnLoad(go);
                    go.AddComponent<LiveKitVoiceManager>();
                    Debug.Log("[VoiceIOBootstrapper] LiveKitVoiceManager created (provider=livekit).");
                }
            }
            else
            {
                if (Object.FindFirstObjectByType<VoiceIOManager>() == null)
                {
                    var go = new GameObject("VoiceIOManager");
                    Object.DontDestroyOnLoad(go);
                    go.AddComponent<VoiceIOManager>();
                    Debug.Log("[VoiceIOBootstrapper] VoiceIOManager created (provider=windows).");
                }
            }
        }
    }
}
