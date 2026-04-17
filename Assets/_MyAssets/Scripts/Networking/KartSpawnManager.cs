using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace PhotonKarts.Networking
{
    /// <summary>
    /// Server-authoritative kart spawner. Called by FusionConnectionManager when
    /// players join or leave the session.
    ///
    /// Uses runner.SetPlayerObject / GetPlayerObject for the player→kart mapping so
    /// the association survives host migration without requiring a NetworkBehaviour.
    /// </summary>
    public class KartSpawnManager : MonoBehaviour
    {
        [Tooltip("Networked kart prefab to spawn for each player.")]
        [SerializeField] private NetworkObject _kartPrefab;

        [Tooltip("Spawn point transforms — one per slot (max 3).")]
        [SerializeField] private Transform[] _spawnPoints;

        // Local slot tracker — rebuilt after host migration.
        private readonly Dictionary<PlayerRef, int> _playerSlots = new();

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

            runner.SetPlayerObject(player, kart);
            _playerSlots[player] = slot;

            Debug.Log($"[Server] Player {player} spawned at slot {slot}.");
        }

        /// <summary>Despawns the kart of a player that left. Server only.</summary>
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.TryGetPlayerObject(player, out var kart))
                runner.Despawn(kart);

            _playerSlots.Remove(player);
            Debug.Log($"[Server] Player {player} kart despawned.");
        }

        /// <summary>
        /// Rebuilds the local slot map after host migration.
        /// Karts are already restored by Fusion — just re-associate slots.
        /// </summary>
        public void OnHostMigrationResume(NetworkRunner runner)
        {
            _playerSlots.Clear();
            int slot = 0;
            foreach (var player in runner.ActivePlayers)
            {
                _playerSlots[player] = slot++;
                Debug.Log($"[Host] Migration resume: player {player} → slot {slot - 1}");
            }
        }

        /// <summary>Returns the kart NetworkObject for a given player, or null.</summary>
        public NetworkObject GetKartForPlayer(NetworkRunner runner, PlayerRef player)
        {
            runner.TryGetPlayerObject(player, out var kart);
            return kart;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private int FindFreeSlot()
        {
            for (int i = 0; i < _spawnPoints.Length; i++)
            {
                if (!IsSlotOccupied(i)) return i;
            }
            return -1;
        }

        private bool IsSlotOccupied(int slot)
        {
            foreach (var s in _playerSlots.Values)
                if (s == slot) return true;
            return false;
        }
    }
}
