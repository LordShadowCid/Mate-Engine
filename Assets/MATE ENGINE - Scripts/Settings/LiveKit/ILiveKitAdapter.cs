using System;
using System.Collections;
using UnityEngine;

namespace MateEngine.Settings.LiveKitBridge
{
    /// <summary>
    /// Abstraction over a LiveKit client. Implementations may be a stub (no SDK)
    /// or a real SDK-backed adapter. All long-running operations are exposed as
    /// coroutines so callers can yield until completion on the main thread.
    /// </summary>
    public interface ILiveKitAdapter
    {
        /// <summary>
        /// True when the adapter is connected to a room and ready for publish/subscribe.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// True when the local microphone is currently being published or unmuted.
        /// </summary>
        bool IsMicPublishing { get; }

        /// <summary>
        /// Connect to a LiveKit server and join a room using a token. Should yield until
        /// connected or failure is known. Implementations should set <see cref="IsConnected"/> accordingly.
        /// </summary>
        IEnumerator Connect(string url, string token);

        /// <summary>
        /// Disconnect from the room and release resources. Should yield until completed.
        /// Implementations should set <see cref="IsConnected"/> to false and clear publish state.
        /// </summary>
        IEnumerator Disconnect();

        /// <summary>
        /// Start or stop publishing the local microphone. Should yield until action is applied.
        /// Implementations should update <see cref="IsMicPublishing"/>.
        /// </summary>
        IEnumerator SetMicPublishing(bool publish);

        // Optional diagnostics
        /// <summary>
        /// Human-readable state for simple on-screen overlays.
        /// </summary>
        string Status { get; }

        /// <summary>
        /// Count of currently visible remote participants (best-effort / optional).
        /// </summary>
        int RemoteParticipantCount { get; }
    }
}
