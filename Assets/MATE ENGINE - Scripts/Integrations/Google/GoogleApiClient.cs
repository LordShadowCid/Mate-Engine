using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace MateEngine.Integrations.Google
{
    [DisallowMultipleComponent]
    public class GoogleApiClient : MonoBehaviour
    {
        public static GoogleApiClient Instance { get; private set; }
        private string lastResult = "";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator AuthorizedGet(string url, Action<string> onDone)
        {
            yield return GoogleAuthManager.Instance.EnsureAccessToken(token =>
            {
                if (string.IsNullOrEmpty(token)) { onDone?.Invoke(null); return; }
                StartCoroutine(AuthorizedGetWithToken(url, token, onDone));
            });
        }

        private IEnumerator AuthorizedGetWithToken(string url, string token, Action<string> onDone)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("Authorization", "Bearer " + token);
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                    onDone?.Invoke(null);
                else
                    onDone?.Invoke(req.downloadHandler.text);
            }
        }

        // Samples
        public void FetchGmailProfile() => StartCoroutine(AuthorizedGet("https://gmail.googleapis.com/gmail/v1/users/me/profile", r => lastResult = r ?? "<error>"));
        public void FetchContacts() => StartCoroutine(AuthorizedGet("https://people.googleapis.com/v1/people/me?personFields=names,emailAddresses", r => lastResult = r ?? "<error>"));
        public void FetchCalendars() => StartCoroutine(AuthorizedGet("https://www.googleapis.com/calendar/v3/users/me/calendarList", r => lastResult = r ?? "<error>"));

        private void OnGUI()
        {
            if (!MateEngine.Settings.ConsentManager.IsGoogleAllowed()) return;
            if (!GoogleAuthManager.Instance || !GoogleAuthManager.Instance.IsSignedIn()) return;

            GUILayout.BeginArea(new Rect(Screen.width - 360, 230, 340, 260), GUI.skin.box);
            GUILayout.Label("Google API (samples)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Gmail Profile")) FetchGmailProfile();
            if (GUILayout.Button("Contacts")) FetchContacts();
            if (GUILayout.Button("Calendars")) FetchCalendars();
            GUILayout.EndHorizontal();

            GUILayout.Label("Result (truncated):");
            string display = string.IsNullOrEmpty(lastResult) ? "<none>" : (lastResult.Length > 500 ? lastResult.Substring(0, 500) + "..." : lastResult);
            GUILayout.TextArea(display);
            GUILayout.EndArea();
        }
    }
}
