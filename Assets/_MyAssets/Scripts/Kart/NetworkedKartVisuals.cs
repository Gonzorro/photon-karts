using Fusion;
using KartGame.KartSystems;
using UnityEngine;

namespace PhotonKarts.Kart
{
    /// <summary>
    /// Visual-only updates for the networked kart. Uses SimulationBehaviour.Render() which
    /// is called once per rendered frame — never during Fusion resimulation ticks.
    ///
    /// Handles:
    ///   - Rotating visual wheel meshes based on current speed.
    ///   - Tilting front wheels for steering visuals.
    /// </summary>
    public class NetworkedKartVisuals : SimulationBehaviour
    {
        [Header("Visual Wheels (meshes, not WheelColliders)")]
        [SerializeField] private Transform _wheelFrontLeft;
        [SerializeField] private Transform _wheelFrontRight;
        [SerializeField] private Transform _wheelRearLeft;
        [SerializeField] private Transform _wheelRearRight;

        [Header("Tuning")]
        [Tooltip("Wheel radius in metres — used to convert linear speed to rotation rate.")]
        [SerializeField] private float _wheelRadius = 0.3f;

        [Tooltip("Max visual steer angle applied to the front wheel meshes (degrees).")]
        [SerializeField] private float _maxSteerAngle = 25f;

        private ArcadeKart _kart;

        // Accumulated rotation (degrees) so wheels roll continuously.
        private float _wheelRotationDeg;

        private void Awake()
        {
            _kart = GetComponent<ArcadeKart>();
        }

        public override void Render()
        {
            if (_kart == null) return;

            UpdateWheelRoll();
            UpdateFrontWheelSteer();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void UpdateWheelRoll()
        {
            // Speed along the kart's forward axis (m/s).
            float speed = Vector3.Dot(_kart.Rigidbody.linearVelocity, transform.forward);

            // Convert linear speed → angular speed (deg/s).
            float angularSpeedDeg = (speed / (2f * Mathf.PI * _wheelRadius)) * 360f;
            _wheelRotationDeg += angularSpeedDeg * Time.deltaTime;

            // Apply roll to all four wheels around their local X axis.
            var roll = Quaternion.Euler(_wheelRotationDeg, 0f, 0f);
            if (_wheelFrontLeft)  _wheelFrontLeft.localRotation  = roll;
            if (_wheelFrontRight) _wheelFrontRight.localRotation = roll;
            if (_wheelRearLeft)   _wheelRearLeft.localRotation   = roll;
            if (_wheelRearRight)  _wheelRearRight.localRotation  = roll;
        }

        private void UpdateFrontWheelSteer()
        {
            if (_kart.Input.TurnInput == 0f) return;

            float steerDeg = _kart.Input.TurnInput * _maxSteerAngle;
            var steer = Quaternion.Euler(_wheelRotationDeg, steerDeg, 0f);
            if (_wheelFrontLeft)  _wheelFrontLeft.localRotation  = steer;
            if (_wheelFrontRight) _wheelFrontRight.localRotation = steer;
        }
    }
}
