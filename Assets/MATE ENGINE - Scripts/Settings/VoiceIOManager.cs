using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech; // DictationRecognizer

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Speech.Synthesis; // Windows TTS
#endif

namespace MateEngine.Settings
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-40)]
    public class VoiceIOManager : MonoBehaviour
    {
        [Header("Voice IO")]
        [Tooltip("Enable Voice IO system at startup")] public bool voiceEnabled = true;
        [Tooltip("Push-To-Talk key (hold to record)")] public KeyCode pushToTalkKey = KeyCode.V;
        [Tooltip("Toggle Voice IO on/off")] public KeyCode toggleVoiceKey = KeyCode.F9;
        [Tooltip("Speak a test phrase")] public KeyCode ttsTestKey = KeyCode.T;

        private DictationRecognizer dictation;
        private bool isRecording = false;
        private string hypothesis = string.Empty;
        private readonly List<string> transcripts = new List<string>();
        private const int MaxLines = 6;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private SpeechSynthesizer tts;
#endif

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Setup TTS (Windows only)
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                tts = new SpeechSynthesizer();
                tts.Rate = 0;
                tts.Volume = 100;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[VoiceIO] TTS init failed: " + e.Message);
            }
#endif

            // Prepare dictation recognizer but don't start yet
            try
            {
                dictation = new DictationRecognizer(ConfidenceLevel.Medium);
                dictation.InitialSilenceTimeoutSeconds = 5f;
                dictation.AutoSilenceTimeoutSeconds = 2f;
                dictation.DictationHypothesis += OnHypothesis;
                dictation.DictationResult += OnResult;
                dictation.DictationComplete += OnComplete;
                dictation.DictationError += OnError;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[VoiceIO] DictationRecognizer init failed: " + e.Message);
            }
        }

        private void Update()
        {
            // Global toggle
            if (Input.GetKeyDown(toggleVoiceKey))
            {
                voiceEnabled = !voiceEnabled;
                StopRecording();
            }

            if (!voiceEnabled)
                return;

            if (!ConsentManager.IsMicAllowed())
            {
                // Don't start recorder if consent not granted
                StopRecording();
                return;
            }

            // Push to talk
            if (Input.GetKeyDown(pushToTalkKey))
            {
                StartRecording();
            }
            else if (Input.GetKeyUp(pushToTalkKey))
            {
                StopRecording();
            }

            // TTS test
            if (Input.GetKeyDown(ttsTestKey))
            {
                Speak("Voice I O is ready.");
            }
        }

        private void StartRecording()
        {
            if (isRecording || dictation == null)
                return;
            try
            {
                hypothesis = string.Empty;
                dictation.Start();
                isRecording = true;
                Debug.Log("[VoiceIO] Recording started");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[VoiceIO] Failed to start dictation: " + e.Message);
            }
        }

        private void StopRecording()
        {
            if (!isRecording || dictation == null)
                return;
            try
            {
                dictation.Stop();
                isRecording = false;
                Debug.Log("[VoiceIO] Recording stopped");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[VoiceIO] Failed to stop dictation: " + e.Message);
            }
        }

        private void OnHypothesis(string text)
        {
            hypothesis = text;
        }

        private void OnResult(string text, ConfidenceLevel confidence)
        {
            AddTranscript(text);
            hypothesis = string.Empty;
            Debug.Log("[VoiceIO] Result: " + text);
        }

        private void OnComplete(DictationCompletionCause cause)
        {
            // Auto-end after silence; we'll flip state on key up too.
            if (cause != DictationCompletionCause.Complete)
            {
                Debug.Log("[VoiceIO] Dictation completed: " + cause);
            }
            isRecording = false;
        }

        private void OnError(string error, int hresult)
        {
            Debug.LogWarning($"[VoiceIO] Dictation error: {error} (0x{hresult:X})");
            isRecording = false;
        }

        private void AddTranscript(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            transcripts.Add(line.Trim());
            while (transcripts.Count > MaxLines)
                transcripts.RemoveAt(0);
        }

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                if (tts != null)
                    tts.SpeakAsync(text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[VoiceIO] TTS speak failed: " + e.Message);
            }
#else
            Debug.Log("[VoiceIO] TTS not available on this platform.");
#endif
        }

        private void OnGUI()
        {
            // Simple HUD
            GUILayout.BeginArea(new Rect(20, Screen.height - 200, 520, 180), GUI.skin.box);
            GUILayout.Label($"Voice: {(voiceEnabled ? "ON" : "OFF")}  |  Consent: {(ConsentManager.IsMicAllowed() ? "Mic OK" : "Mic Blocked")}");
            GUILayout.Label($"PTT: Hold {pushToTalkKey}  |  Toggle: {toggleVoiceKey}  |  TTS Test: {ttsTestKey}");
            if (isRecording)
            {
                GUILayout.Label("Listening...");
                if (!string.IsNullOrEmpty(hypothesis))
                    GUILayout.Label(".. " + hypothesis);
            }
            if (transcripts.Count > 0)
            {
                GUILayout.Label("Recent:");
                for (int i = 0; i < transcripts.Count; i++)
                    GUILayout.Label("- " + transcripts[i]);
            }
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            try
            {
                if (dictation != null)
                {
                    if (isRecording) dictation.Stop();
                    dictation.DictationHypothesis -= OnHypothesis;
                    dictation.DictationResult -= OnResult;
                    dictation.DictationComplete -= OnComplete;
                    dictation.DictationError -= OnError;
                    dictation.Dispose();
                }
            }
            catch { }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try { tts?.Dispose(); } catch { }
#endif
        }
    }
}
