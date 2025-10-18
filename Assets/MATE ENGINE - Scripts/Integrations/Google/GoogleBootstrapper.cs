using UnityEngine;

namespace MateEngine.Integrations.Google
{
    public static class GoogleBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureGoogleManagers()
        {
            if (Object.FindFirstObjectByType<GoogleAuthManager>() == null)
            {
                var go = new GameObject("GoogleAuthManager");
                Object.DontDestroyOnLoad(go);
                go.AddComponent<GoogleAuthManager>();
                Debug.Log("[GoogleBootstrapper] GoogleAuthManager created.");
            }

            if (Object.FindFirstObjectByType<GoogleApiClient>() == null)
            {
                var go = new GameObject("GoogleApiClient");
                Object.DontDestroyOnLoad(go);
                go.AddComponent<GoogleApiClient>();
                Debug.Log("[GoogleBootstrapper] GoogleApiClient created.");
            }
        }
    }
}
