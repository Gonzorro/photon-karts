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

        public void Reset()
        {
            Status      = ConnectionStatus.Disconnected;
            RoomName    = string.Empty;
            IsHost      = false;
            PlayerCount = 0;
            MaxPlayers  = 0;
            LocalPlayer = string.Empty;
        }
    }
}
