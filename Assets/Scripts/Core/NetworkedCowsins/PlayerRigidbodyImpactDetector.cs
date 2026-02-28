using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Listens to the Player's main Rigidbody OnCollisionEnter and triggers ragdoll on high-speed impact.
    /// This is the primary path for impact detection because body-part trigger colliders are inside
    /// the main capsule and never reach obstacles (the capsule hits first).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class PlayerRigidbodyImpactDetector : MonoBehaviour
    {
        [Header("Impact Threshold")]
        [Tooltip("Minimum impact speed (relativeVelocity.magnitude) to trigger ragdoll. Raised to 9 so minor bumps don't trigger.")]
        [SerializeField] private float highSpeedThreshold = 9f;
        [Tooltip("Scale applied to force magnitude when threshold exceeded.")]
        [SerializeField] private float impactForceScale = 1f;
        [Tooltip("Minimum seconds between ragdoll triggers.")]
        [SerializeField] private float impactCooldownSeconds = 0.2f;

        private NetworkedCowsinsRagdollController _ragdollController;
        private NetworkedCowsinsPlayerController _playerController;
        private float _lastImpactTime;

        private void Start()
        {
            _ragdollController = GetComponentInParent<NetworkedCowsinsRagdollController>();
            _playerController = GetComponentInParent<NetworkedCowsinsPlayerController>();
            if (_playerController != null && !_playerController.IsOwner)
                enabled = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_ragdollController == null || _playerController == null || !_playerController.IsOwner)
                return;
            if (collision.transform.IsChildOf(_playerController.transform))
                return;

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed <= highSpeedThreshold)
                return;
            if (Time.unscaledTime - _lastImpactTime < impactCooldownSeconds)
                return;

            _lastImpactTime = Time.unscaledTime;
            Vector3 forceVector = collision.relativeVelocity.normalized * (impactSpeed - highSpeedThreshold) * impactForceScale;
            if (forceVector.sqrMagnitude < 0.01f)
                forceVector = -transform.forward * 5f;

            _ragdollController.TriggerRagdollFromImpactServerRpc(forceVector, impactSpeed);
        }
    }
}
