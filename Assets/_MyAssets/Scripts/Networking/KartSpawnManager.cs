using System.Linq;
using Fusion;
using UnityEngine;

namespace PhotonKarts.Networking
{
    /// <summary>
    /// Server-authoritative kart spawner. Called by FusionConnectionManager when
    /// players join or leave the session.
    ///
    /// Karts are indexed by spawn slot (0-2) rather than PlayerRef because PlayerRef
    /// IDs can change after host migration. SlottedKarts survives migration via the
    /// HostMigrationToken snapshot since it is a [Networked] NetworkArray.
    /// </summary>
    public class KartSpawnManager : NetworkBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Tooltip("Networked kart prefab to spawn for each player.")]
        [SerializeField] private NetworkObject _kartPrefab;

        [Tooltip("Spawn point transforms — one per slot (max 3).")]
        [SerializeField] private Transform[] _spawnPoints;

        // ── Networked state ───────────────────────────────────────────────────────

        /// <summary>
        /// Slot-indexed kart references. Index 0-2 matches _spawnPoints.
        /// Stored as a NetworkArray so the mapping survives host migration.
        /// </summary>
        [Networked, Capacity(3)]
        private NetworkArray<NetworkObject> SlottedKarts => default;

        // ── Runtime ───────────────────────────────────────────────────────────────

        // Maps PlayerRef → slot index for quick lookup (local, not networked).
        private readonly System.Collections.Generic.Dictionary<PlayerRef, int> _playerSlots
            = new System.Collections.Generic.Dictionary<PlayerRef, int>();

        // ── Public API (called by FusionConnectionManager) ────────────────────────

        /// <summary>Spawns a kart for the joining player. Server only.</summary>
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            int slot = FindFreeSlot();
            if (slot < 0)
            {
                Debug.LogWarning($"[KartSpawnManager] No free slot for player {player}.");
                return;
            }

            var spawnPoint = _spawnPoints[slot];
            var kart = runner.Spawn(
                _kartPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                inputAuthority: player
            );

            SlottedKarts.Set(slot, kart);
            _playerSlots[player] = slot;

            Debug.Log($"[Server] Player {player} spawned at slot {slot}. " +
                      $"Players in session: {runner.ActivePlayers.Count()}");
        }

        /// <summary>Despawns the kart of a player that left. Server only.</summary>
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (!_playerSlots.TryGetValue(player, out int slot)) return;

            var kart = SlottedKarts.Get(slot);
            if (kart != null)
                runner.Despawn(kart);

            SlottedKarts.Set(slot, null);
            _playerSlots.Remove(player);

            Debug.Log($"[Server] Player {player} kart despawned from slot {slot}.");
        }

        /// <summary>
        /// Called by FusionConnectionManager after host migration completes.
        /// Re-maps PlayerRef → slot from the token-restored SlottedKarts array.
        /// </summary>
        public void OnHostMigrationResume(NetworkRunner runner)
        {
            _playerSlots.Clear();

            for (int slot = 0; slot < SlottedKarts.Length; slot++)
            {
                var kart = SlottedKarts.Get(slot);
                if (kart == null) continue;

                _playerSlots[kart.InputAuthority] = slot;
                Debug.Log($"[Host] Migration resume: player {kart.InputAuthority} → slot {slot}");
            }
        }

        /// <summary>Returns the kart NetworkObject for a given player, or null.</summary>
        public NetworkObject GetKartForPlayer(PlayerRef player)
        {
            if (!_playerSlots.TryGetValue(player, out int slot)) return null;
            return SlottedKarts.Get(slot);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private int FindFreeSlot()
        {
            for (int i = 0; i < _spawnPoints.Length && i < SlottedKarts.Length; i++)
            {
                if (SlottedKarts.Get(i) == null)
                    return i;
            }
            return -1;
        }
    }
}
