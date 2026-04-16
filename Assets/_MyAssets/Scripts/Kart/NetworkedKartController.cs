using Fusion;
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
    /// Position/rotation sync and interpolation is handled by NetworkTransform (same GameObject).
    /// </summary>
    [RequireComponent(typeof(ArcadeKart))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(FusionInputProxy))]
    public class NetworkedKartController : NetworkBehaviour
    {
        private ArcadeKart _kart;
        private FusionInputProxy _proxy;

        // ── Fusion lifecycle ──────────────────────────────────────────────────────

        public override void Spawned()
        {
            _kart  = GetComponent<ArcadeKart>();
            _proxy = GetComponent<FusionInputProxy>();

            // Proxies are purely visual — NetworkTransform interpolates their transform.
            // Disabling ArcadeKart stops its FixedUpdate from fighting the interpolated state.
            if (!HasStateAuthority && !HasInputAuthority)
                _kart.enabled = false;
        }

        public override void FixedUpdateNetwork()
        {
            // Proxies skip simulation entirely.
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
