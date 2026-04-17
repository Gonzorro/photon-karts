using PhotonKarts.Networking;
using TMPro;
using UnityEngine;

namespace PhotonKarts.UI
{
    /// <summary>
    /// Displays live connection state from ConnectionStateSO on screen.
    /// </summary>
    public class ConnectionHUD : MonoBehaviour
    {
        [SerializeField] private ConnectionStateSO _state;
        [SerializeField] private TextMeshProUGUI   _text;

        private void Update()
        {
            if (_state == null || _text == null) return;
            _text.text = BuildText();
        }

        private string BuildText()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"Status:  {_state.Status}");
            sb.AppendLine($"Room:    {(_state.RoomName.Length > 0 ? _state.RoomName : "—")}");
            sb.AppendLine($"Role:    {(_state.IsHost ? "Host" : "Client")}");
            sb.AppendLine($"Players: {_state.PlayerCount} / {_state.MaxPlayers}");
            sb.AppendLine($"Local:   {(_state.LocalPlayer.Length > 0 ? _state.LocalPlayer : "—")}");

            return sb.ToString();
        }
    }
}
