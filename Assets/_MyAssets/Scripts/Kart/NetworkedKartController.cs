using Fusion;
using Fusion.Addons.Physics;
using KartGame.KartSystems;
using PhotonKarts.Networking;
using UnityEngine;

namespace PhotonKarts.Kart
{
    /// <summary>
    /// Fusion 2 network layer for the kart. Sits alongside ArcadeKart on the kart prefab.
    ///
    /// Responsibilities:
    ///   - Reads NetworkInputData each tick and forwards it to FusionInputProxy so ArcadeKart
    ///     picks it up via its IInput[] array (set up in ArcadeKart.Awake).
    ///   - Disables ArcadeKart physics on proxy karts (non-authority, non-input-owner) so
    ///     they don't fight NetworkRigidbody3D interpolation.
    ///   - Exposes Freeze/Unfreeze for host-migration overlay.
    ///
    /// Position/rotation sync and prediction reconciliation is handled by NetworkRigidbody3D (same GameObject).
    /// </summary>
    [RequireComponent(typeof(ArcadeKart))]
    [RequireComponent(typeof(NetworkRigidbody3D))]
    [RequireComponent(typeof(FusionInputProxy))]
    public class NetworkedKartController : NetworkBehaviour
    {
        /// <summary>Set when the local input-authority kart spawns. Cleared on despawn.</summary>
        public static NetworkedKartController LocalKart { get; private set; }

        private ArcadeKart _kart;
        private FusionInputProxy _proxy;

        // ── Fusion lifecycle ──────────────────────────────────────────────────────

        public override void Spawned()
        {
            _kart  = GetComponent<ArcadeKart>();
            _proxy = GetComponent<FusionInputProxy>();

            if (!HasStateAuthority && !HasInputAuthority)
            {
                _kart.enabled = false;
            }
            else if (HasInputAuthority)
            {
                // Client prediction — opt into local physics simulation.
                Runner.SetIsSimulated(Object, true);
                var rb = GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;
            }

            // Set camera for whoever owns this kart locally — works on both host and client.
            if (Object.InputAuthority == Runner.LocalPlayer)
            {
                LocalKart = this;
                var cam = FindFirstObjectByType<KartCamera>();
                if (cam != null) cam.SetTarget(transform);
                else Debug.LogWarning("[NKC] KartCamera not found in scene!");
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (LocalKart == this) LocalKart = null;
        }

        private void LateUpdate()
        {
            if (_kart == null || !_kart.enabled) return;

            var gfm    = NetworkGameFlowManager.Instance;
            bool canMove = gfm == null || gfm.Phase == RacePhase.Racing || gfm.Phase == RacePhase.Finished;
            _kart.SetCanMove(canMove);
        }

        public override void FixedUpdateNetwork()
        {
            // Runs on both host (authoritative) and input-authority client (predicted).
            // Proxies skip — they have no input and no physics to run.
            if (!HasStateAuthority && !HasInputAuthority) return;

            if (GetInput(out NetworkInputData input))
                _proxy.SetInput(input);
        }

        // ── Host migration ────────────────────────────────────────────────────────

        /// <summary>
        /// Freeze kart input and physics. Called on OnHostMigration before the runner shuts down.
        /// </summary>
        public void Freeze()   => _kart.SetCanMove(false);

        /// <summary>
        /// Re-enable movement after host migration countdown completes.
        /// </summary>
        public void Unfreeze() => _kart.SetCanMove(true);
    }
}
