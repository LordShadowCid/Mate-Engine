using System.Collections;
using UnityEngine;

namespace MateEngine.Settings.LiveKitBridge
{
    // Works without SDK; simulates connection and mic publish behavior
    public class LiveKitAdapterStub : ILiveKitAdapter
    {
        public bool IsConnected { get; private set; }
        public bool IsMicPublishing { get; private set; }
        public string Status { get; private set; } = "idle";
        public int RemoteParticipantCount { get; private set; } = 0;

        public IEnumerator Connect(string url, string token)
        {
            Status = "connecting (stub)";
            yield return new WaitForSeconds(0.25f);
            IsConnected = true;
            Status = "connected (stub)";
        }

        public IEnumerator Disconnect()
        {
            Status = "disconnecting";
            yield return new WaitForSeconds(0.15f);
            IsConnected = false;
            IsMicPublishing = false;
            Status = "disconnected";
        }

        public IEnumerator SetMicPublishing(bool publish)
        {
            IsMicPublishing = publish;
            yield break;
        }
    }
}
