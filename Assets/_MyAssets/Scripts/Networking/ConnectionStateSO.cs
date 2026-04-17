using System.Collections.Generic;
using UnityEngine;

namespace PhotonKarts.Networking
{
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Failed
    }

    /// <summary>
    /// Shared data channel between FusionConnectionManager and the ConnectionHUD.
    /// Updated by the networking layer, read by UI — no direct references needed.
    /// </summary>
    [CreateAssetMenu(menuName = "PhotonKarts/Connection State", fileName = "ConnectionStateSO")]
    public class ConnectionStateSO : ScriptableObject
    {
        public ConnectionStatus Status;
        public string           RoomName;
        public bool             IsHost;
        public int              PlayerCount;
        public int              MaxPlayers;
        public string           LocalPlayer;

        /// <summary>Live connection log — newest entry first.</summary>
        public readonly List<string> Log = new List<string>();
        private const int MaxLogLines = 8;

        public void PushLog(string message)
        {
            Log.Insert(0, $"[{System.DateTime.Now:HH:mm:ss}] {message}");
            if (Log.Count > MaxLogLines) Log.RemoveAt(Log.Count - 1);
        }

        public void Reset()
        {
            Status      = ConnectionStatus.Disconnected;
            RoomName    = string.Empty;
            IsHost      = false;
            PlayerCount = 0;
            MaxPlayers  = 0;
            LocalPlayer = string.Empty;
            Log.Clear();
        }
    }
}
