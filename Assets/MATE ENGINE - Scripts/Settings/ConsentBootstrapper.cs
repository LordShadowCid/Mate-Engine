using UnityEngine;

namespace MateEngine.Settings
{
    public static class ConsentBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsureConsentManager()
        {
            if (Object.FindFirstObjectByType<ConsentManager>() == null)
            {
                var go = new GameObject("ConsentManager");
                Object.DontDestroyOnLoad(go);
                go.AddComponent<ConsentManager>();
                Debug.Log("[ConsentBootstrapper] ConsentManager created at startup.");
            }
        }
    }
}
