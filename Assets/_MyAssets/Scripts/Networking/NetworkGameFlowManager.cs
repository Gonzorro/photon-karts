using System.Linq;
using Fusion;
using UnityEngine;

namespace PhotonKarts.Networking
{
    public enum RacePhase { WaitingForReady, Countdown, Racing, Finished }

    /// <summary>
    /// Host-authoritative race flow. Manages ready-up, countdown and phase transitions.
    /// Add to a NetworkObject in the scene alongside NetworkManager.
    /// </summary>
    public class NetworkGameFlowManager : NetworkBehaviour
    {
        public static NetworkGameFlowManager Instance { get; private set; }

        [SerializeField] private float _countdownSeconds = 3f;
        [SerializeField] private int   _minPlayersToStart = 2;

        [Networked] public  RacePhase  Phase        { get; private set; }
        [Networked] public  int        ReadyBitmask { get; private set; }
        [Networked] public  TickTimer  Countdown    { get; private set; }

        // ── Fusion lifecycle ──────────────────────────────────────────────────────

        public override void Spawned()
        {
            Instance = this;
            if (HasStateAuthority)
                Phase = RacePhase.WaitingForReady;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this) Instance = null;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            switch (Phase)
            {
                case RacePhase.WaitingForReady:
                    if (AllPlayersReady())
                    {
                        Phase     = RacePhase.Countdown;
                        Countdown = TickTimer.CreateFromSeconds(Runner, _countdownSeconds);
                        Debug.Log("[GameFlow] All ready — countdown started.");
                    }
                    break;

                case RacePhase.Countdown:
                    if (Countdown.Expired(Runner))
                    {
                        Phase = RacePhase.Racing;
                        Debug.Log("[GameFlow] Race started!");
                    }
                    break;
            }
        }

        // ── RPC ───────────────────────────────────────────────────────────────────

        /// <summary>Called by any peer to toggle their ready state.</summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_ToggleReady(RpcInfo info = default)
        {
            if (Phase != RacePhase.WaitingForReady) return;

            int bit      = 1 << (info.Source.PlayerId - 1);
            ReadyBitmask ^= bit;

            Debug.Log($"[GameFlow] Player {info.Source} toggled ready. Bitmask={ReadyBitmask}");
        }

        // ── Public queries ────────────────────────────────────────────────────────

        public bool IsPlayerReady(PlayerRef player)
            => (ReadyBitmask & (1 << (player.PlayerId - 1))) != 0;

        public float CountdownRemaining
            => Countdown.IsRunning ? Countdown.RemainingTime(Runner) ?? 0f : 0f;

        // ── Helpers ───────────────────────────────────────────────────────────────

        private bool AllPlayersReady()
        {
            var players = Runner.ActivePlayers.ToList();
            if (players.Count < _minPlayersToStart) return false;
            return players.All(IsPlayerReady);
        }
    }
}
