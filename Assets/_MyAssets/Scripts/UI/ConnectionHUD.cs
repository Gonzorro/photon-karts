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

            if (_state.Log.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("── Connection Log ──");
                foreach (var line in _state.Log)
                    sb.AppendLine(line);
            }

            AppendRaceFlow(sb);

            return sb.ToString();
        }

        private void AppendRaceFlow(System.Text.StringBuilder sb)
        {
            var gfm = NetworkGameFlowManager.Instance;
            if (gfm == null) return;

            sb.AppendLine();

            switch (gfm.Phase)
            {
                case RacePhase.WaitingForReady:
                    sb.AppendLine("── Ready Up (SPACE) ──");
                    for (int i = 0; i < _state.PlayerCount; i++)
                    {
                        // PlayerRef.PlayerId is 1-indexed
                        bool ready = (gfm.ReadyBitmask & (1 << i)) != 0;
                        sb.AppendLine($"  Player {i + 1}: {(ready ? "✓ READY" : "waiting...")}");
                    }
                    break;

                case RacePhase.Countdown:
                    sb.AppendLine($"  Starting in {gfm.CountdownRemaining:F1}s");
                    break;

                case RacePhase.Racing:
                    sb.AppendLine("  RACING");
                    break;

                case RacePhase.Finished:
                    sb.AppendLine("  FINISHED");
                    break;
            }
        }
    }
}
