using UnityEngine;

namespace PhotonKarts.Kart
{
    /// <summary>
    /// Follows the local player's kart. Assign via SetTarget() called from NetworkedKartController.
    /// </summary>
    public class KartCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 _offset     = new Vector3(0f, 3f, -6f);
        [SerializeField] private float   _smoothSpeed = 10f;

        private Transform _target;

        private void Update()
        {
            if (_target == null) return;

            // Rotate offset by kart's yaw only — avoids jitter from pitch/roll.
            Quaternion yawOnly = Quaternion.Euler(0f, _target.eulerAngles.y, 0f);
            Vector3 desired = _target.position + yawOnly * _offset;

            transform.position = Vector3.Lerp(
                transform.position, desired, _smoothSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.LookRotation(_target.position - transform.position + Vector3.up * 0.5f),
                _smoothSpeed * Time.deltaTime);
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }
    }
}
