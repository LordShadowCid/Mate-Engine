using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace MateEngine.Settings.LiveKitBridge
{
    // Placeholder for the real SDK-backed adapter; compiled only when LIVEKIT_SDK is defined
    public class LiveKitAdapterSdk : ILiveKitAdapter
    {
        public bool IsConnected { get; private set; }
        public bool IsMicPublishing { get; private set; }
        public string Status { get; private set; } = "idle";
        public int RemoteParticipantCount { get; private set; } = 0;

#if LIVEKIT_SDK
        // TODO: Replace placeholder types with actual LiveKit C# SDK types when installed
        // Example (pseudo):
        // private LiveKit.Room _room;
        // private LiveKit.LocalAudioTrack _micTrack;
        // private LiveKit.LocalTrackPublication _micPublication;
        private object _room;
        private object _micTrack;
        private object _micPublication;

        // Keep last connect args for optional reconnects
        private string _lastUrl;
        private string _lastToken;
#endif

        public IEnumerator Connect(string url, string token)
        {
#if LIVEKIT_SDK
            if (IsConnected)
            {
                yield break;
            }

            Status = "connecting";
            _lastUrl = url;
            _lastToken = token;

            Exception caught = null;
            yield return AwaitTask(ConnectAsync(url, token), ex => caught = ex);

            if (caught != null)
            {
                Status = "error: " + caught.Message;
                IsConnected = false;
                yield break;
            }

            IsConnected = true;
            Status = "connected";
#else
            Debug.LogWarning("[LiveKitAdapterSdk] LIVEKIT_SDK not defined; falling back not possible here.");
#endif
            yield break;
        }

        public IEnumerator Disconnect()
        {
#if LIVEKIT_SDK
            Status = "disconnecting";
            Exception caught = null;
            yield return AwaitTask(DisconnectAsync(), ex => caught = ex);
            if (caught != null)
            {
                Status = "error: " + caught.Message;
                yield break;
            }

            IsConnected = false;
            IsMicPublishing = false;
            Status = "disconnected";
#else
            yield break;
#endif
        }

        public IEnumerator SetMicPublishing(bool publish)
        {
#if LIVEKIT_SDK
            Exception caught = null;
            yield return AwaitTask(SetMicPublishingAsync(publish), ex => caught = ex);
            if (caught == null)
            {
                IsMicPublishing = publish;
            }
#else
            yield break;
#endif
        }

#if LIVEKIT_SDK
        // ===== Async implementations (to be mapped to actual LiveKit SDK) =====
        private async Task ConnectAsync(string url, string token)
        {
            // PSEUDO-CODE — replace with actual LiveKit SDK calls
            // _room = await LiveKit.Room.ConnectAsync(url, token);
            // Hook events
            // _room.ParticipantConnected += OnParticipantConnected;
            // _room.ParticipantDisconnected += OnParticipantDisconnected;
            await Task.Yield();
        }

        private async Task DisconnectAsync()
        {
            // PSEUDO-CODE — replace with actual LiveKit SDK calls
            // if (_micPublication != null) await _micPublication.UnpublishAsync();
            // if (_micTrack != null) await _micTrack.StopAsync();
            // if (_room != null) await _room.DisconnectAsync();
            // _room = null; _micTrack = null; _micPublication = null;
            await Task.Yield();
        }

        private async Task SetMicPublishingAsync(bool publish)
        {
            // PSEUDO-CODE — replace with actual LiveKit SDK calls
            // if (_room == null) throw new InvalidOperationException("Not connected");
            // if (publish)
            // {
            //     if (_micTrack == null) _micTrack = await LiveKit.LocalAudioTrack.CreateMicrophoneTrackAsync();
            //     if (_micPublication == null) _micPublication = await _room.LocalParticipant.PublishTrackAsync(_micTrack);
            // }
            // else
            // {
            //     if (_micPublication != null) { await _micPublication.UnpublishAsync(); _micPublication = null; }
            // }
            await Task.Yield();
        }

        // Example event handlers to maintain RemoteParticipantCount
        private void OnParticipantConnected(/* actual participant type */)
        {
            RemoteParticipantCount++;
        }

        private void OnParticipantDisconnected(/* actual participant type */)
        {
            RemoteParticipantCount = Mathf.Max(0, RemoteParticipantCount - 1);
        }

        // ===== Coroutine helpers for awaiting Tasks on main thread =====
        private static IEnumerator AwaitTask(Task task, Action<Exception> onException = null)
        {
            if (task == null) yield break;
            while (!task.IsCompleted)
            {
                yield return null;
            }
            if (task.IsFaulted && onException != null)
            {
                onException(task.Exception?.InnerException ?? task.Exception);
            }
        }
#endif
    }
}
