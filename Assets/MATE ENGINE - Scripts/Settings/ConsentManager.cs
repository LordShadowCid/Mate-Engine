using UnityEngine;

namespace MateEngine.Settings
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public class ConsentManager : MonoBehaviour
    {
        private bool showOverlay;
        private Rect windowRect = new Rect(40, 40, 520, 420);

        private void Start()
        {
            var data = SaveLoadHandler.Instance?.data;
            if (data == null)
            {
                Debug.LogWarning("[ConsentManager] SaveLoadHandler not initialized.");
                return;
            }
            // Show overlay if never shown or consent version changed
            showOverlay = !data.consentShown || data.consentVersion < 1;
        }

        private void OnGUI()
        {
            if (!showOverlay) return;
            windowRect = GUILayout.Window(987654, windowRect, DrawWindow, "Mate Engine - Privacy & Consent");
        }

        private void DrawWindow(int id)
        {
            var data = SaveLoadHandler.Instance.data;
            GUILayout.Label("Please review and consent to the features you want to enable. You can change this later in Settings.");
            GUILayout.Space(8);

            data.allowMicrophone = GUILayout.Toggle(data.allowMicrophone, "Enable microphone access (voice commands, STT)");
            data.allowScreenCapture = GUILayout.Toggle(data.allowScreenCapture, "Enable screen capture (for OCR and screen watching)");
            data.allowWebcam = GUILayout.Toggle(data.allowWebcam, "Enable webcam (for face recognition)");
            data.allowGoogleAccess = GUILayout.Toggle(data.allowGoogleAccess, "Enable Google account access (Gmail, Contacts, Calendar)");
            data.allowBackgroundLogging = GUILayout.Toggle(data.allowBackgroundLogging, "Allow anonymized usage logging for self-learning features (local only)");

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Allow Selected"))
            {
                data.consentShown = true;
                data.consentVersion = 1;
                SaveLoadHandler.Instance.SaveToDisk();
                showOverlay = false;
            }
            if (GUILayout.Button("Disable All"))
            {
                data.allowMicrophone = false;
                data.allowScreenCapture = false;
                data.allowWebcam = false;
                data.allowGoogleAccess = false;
                data.allowBackgroundLogging = false;
                data.consentShown = true;
                data.consentVersion = 1;
                SaveLoadHandler.Instance.SaveToDisk();
                showOverlay = false;
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        // Static gates for other systems
        public static bool IsMicAllowed() => SaveLoadHandler.Instance?.data?.allowMicrophone == true;
        public static bool IsScreenAllowed() => SaveLoadHandler.Instance?.data?.allowScreenCapture == true;
        public static bool IsWebcamAllowed() => SaveLoadHandler.Instance?.data?.allowWebcam == true;
        public static bool IsGoogleAllowed() => SaveLoadHandler.Instance?.data?.allowGoogleAccess == true;
        public static bool IsLoggingAllowed() => SaveLoadHandler.Instance?.data?.allowBackgroundLogging == true;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                showOverlay = true;
            }
        }
    }
}
