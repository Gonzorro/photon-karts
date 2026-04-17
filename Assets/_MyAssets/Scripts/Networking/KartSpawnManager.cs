using System.Collections.Generic;
using Fusion;
using PhotonKarts.Kart;
using UnityEngine;

namespace PhotonKarts.Networking
{
    /// <summary>
    /// Server-authoritative kart spawner. Called by FusionConnectionManager when
    /// players join or leave the session.
    ///
    /// Uses runner.SetPlayerObject / GetPlayerObject for the player→kart mapping.
    /// On host migration, kart transforms are captured before the runner shuts down
    /// and used to re-spawn karts at their last positions instead of spawn points.
    /// </summary>
    public class KartSpawnManager : MonoBehaviour
    {
        [Tooltip("Networked kart prefab to spawn for each player.")]
        [SerializeField] private NetworkObject _kartPrefab;

        [Tooltip("Spawn point transforms — one per slot (max 3).")]
        [SerializeField] private Transform[] _spawnPoints;

        private readonly Dictionary<PlayerRef, int>                           _playerSlots        = new();
        private readonly Dictionary<PlayerRef, (Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)> _migrationPositions = new();

        // ── Public API (called by FusionConnectionManager) ────────────────────────

        /// <summary>
        /// Captures every kart's world transform keyed by InputAuthority.
        /// Must be called in OnHostMigration BEFORE the runner shuts down.
        /// </summary>
        public void SaveMigrationPositions()
        {
            _migrationPositions.Clear();
            var karts = Object.FindObjectsByType<NetworkedKartController>(FindObjectsSortMode.None);
            foreach (var kart in karts)
            {
                if (kart.Object == null || kart.Object.InputAuthority == PlayerRef.None) continue;
                var rb = kart.GetComponent<Rigidbody>();
                _migrationPositions[kart.Object.InputAuthority] = (
                    kart.transform.position,
                    kart.transform.rotation,
                    rb != null ? rb.linearVelocity    : Vector3.zero,
                    rb != null ? rb.angularVelocity : Vector3.zero
                );
            }
            Debug.Log($"[KartSpawnManager] Saved {_migrationPositions.Count} kart positions for migration.");
        }

        /// <summary>Spawns a kart for the joining player. Server only.</summary>
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Vector3    spawnPos;
            Quaternion spawnRot;
            Vector3    spawnVel    = Vector3.zero;
            Vector3    spawnAngVel = Vector3.zero;

            if (_migrationPositions.TryGetValue(player, out var saved))
            {
                spawnPos    = saved.pos;
                spawnRot    = saved.rot;
                spawnVel    = saved.vel;
                spawnAngVel = saved.angVel;
                _migrationPositions.Remove(player);
                Debug.Log($"[KartSpawnManager] Player {player} restored to migration position.");
            }
            else
            {
                int slot = FindFreeSlot();
                if (slot < 0)
                {
                    Debug.LogWarning($"[KartSpawnManager] No free slot for player {player}.");
                    return;
                }
                spawnPos             = _spawnPoints[slot].position;
                spawnRot             = _spawnPoints[slot].rotation;
                _playerSlots[player] = slot;
                Debug.Log($"[KartSpawnManager] Player {player} spawned at slot {slot}.");
            }

            var kart = runner.Spawn(_kartPrefab, spawnPos, spawnRot, inputAuthority: player);
            runner.SetPlayerObject(player, kart);

            if (spawnVel != Vector3.zero || spawnAngVel != Vector3.zero)
            {
                var rb = kart.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity  = spawnVel;
                    rb.angularVelocity = spawnAngVel;
                }
            }
        }

        /// <summary>Despawns the kart of a player that left. Server only.</summary>
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.TryGetPlayerObject(player, out var kart))
                runner.Despawn(kart);

            _playerSlots.Remove(player);
            _migrationPositions.Remove(player);
            Debug.Log($"[Server] Player {player} kart despawned.");
        }

        /// <summary>
        /// Rebuilds player→kart mappings on the new runner after migration.
        /// Fresh karts are spawned via OnPlayerJoined using saved positions.
        /// </summary>
        public void OnHostMigrationResume(NetworkRunner runner)
        {
            _playerSlots.Clear();

            var karts = Object.FindObjectsByType<NetworkedKartController>(FindObjectsSortMode.None);
            int slot = 0;
            foreach (var kart in karts)
            {
                var player = kart.Object != null ? kart.Object.InputAuthority : PlayerRef.None;
                if (player == PlayerRef.None) continue;
                runner.SetPlayerObject(player, kart.Object);
                _playerSlots[player] = slot++;
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
