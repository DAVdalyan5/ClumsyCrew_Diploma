using cowsins;
using R3;
using R3.Triggers;
using System.Collections.Generic;
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
        [Tooltip("Time after landing during which ragdoll triggers are ignored.")]
        [SerializeField] private float postLandingGracePeriod = 0.3f;
        [Tooltip("Ignore collisions from objects below the player (ground-like).")]
        [SerializeField] private bool ignoreCollisionsBelowPlayer = true;
        [Tooltip("Angle threshold (degrees) for 'below player' check. Higher = more lenient.")]
        [SerializeField] private float belowPlayerAngleThreshold = 45f;
        [Tooltip("Colliders to capture ragdoll triggers")]
        [SerializeField] private List<Collider> collidersToCapture;

        private NetworkedCowsinsRagdollController _ragdollController;
        private NetworkedCowsinsPlayerController _playerController;
        private cowsins.PlayerMovement _playerMovement;
        private float _lastImpactTime;
        private float _lastLandingTime;
        private bool _wasGroundedLastFrame;

        private void Start()
        {
            _ragdollController = GetComponentInParent<NetworkedCowsinsRagdollController>();
            _playerController = GetComponentInParent<NetworkedCowsinsPlayerController>();
            if (_playerController != null && !_playerController.IsOwner)
            {
                enabled = false;
                return;
            }

            _playerMovement = _playerController?.GetPlayerMovement();
            _wasGroundedLastFrame = _playerMovement != null && _playerMovement.Grounded;
            _lastLandingTime = -postLandingGracePeriod; // Allow immediate triggers at start

            collidersToCapture.ForEach(c => c.OnTriggerEnterAsObservable().Subscribe(CollisionEntered));
        }

        private void Update()
        {
            if (_playerMovement == null) return;

            bool isGroundedNow = _playerMovement.Grounded;
            if (isGroundedNow && !_wasGroundedLastFrame)
            {
                // Just landed
                _lastLandingTime = Time.unscaledTime;
            }
            _wasGroundedLastFrame = isGroundedNow;
        }

        /// <summary>
        /// Checks if a collider is below the player (ground-like).
        /// Uses angle threshold to determine if the collision is from below.
        /// </summary>
        private bool IsBelowPlayer(Collider other, Transform playerTransform)
        {
            Vector3 toOther = other.bounds.center - playerTransform.position;
            // Convert angle threshold to dot product threshold
            float dotThreshold = Mathf.Cos(belowPlayerAngleThreshold * Mathf.Deg2Rad);
            return Vector3.Dot(toOther.normalized, Vector3.down) > dotThreshold;
        }

        private void CollisionEntered(Collider other)
        {
            if (_ragdollController == null || _playerController == null || !_playerController.IsOwner)
                return;
            if (other.transform.IsChildOf(_playerController.transform))
                return;
            if (other.CompareTag("Ground"))
                return;

            // Skip if player is currently grounded - don't ragdoll during normal running
            if (_playerMovement != null && _playerMovement.Grounded)
                return;

            // Skip collisions from objects below the player (ground-like impacts)
            if (ignoreCollisionsBelowPlayer && IsBelowPlayer(other, _playerController.transform))
                return;

            // Skip during post-landing grace period (physics settling after a jump/fall)
            if (Time.unscaledTime - _lastLandingTime < postLandingGracePeriod)
                return;

            var rb = GetComponent<Rigidbody>();
            if (rb == null) return;

            // Only use horizontal velocity - vertical landing velocity shouldn't trigger ragdoll
            var planarVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float impactSpeed = planarVelocity.magnitude;
            if (impactSpeed <= highSpeedThreshold)
                return;
            if (Time.unscaledTime - _lastImpactTime < impactCooldownSeconds)
                return;

            _lastImpactTime = Time.unscaledTime;
            Vector3 forceVector = planarVelocity.normalized * (impactSpeed - highSpeedThreshold) * impactForceScale;
            if (forceVector.sqrMagnitude < 0.01f)
                forceVector = -transform.forward * 5f;

            Debug.Log($"[PlayerRigidbodyImpactDetector] Ragdoll triggered - Collider: {other.name}, Speed: {impactSpeed:F2}, Threshold: {highSpeedThreshold}");

            _ragdollController.TriggerRagdollFromImpactServerRpc(forceVector, impactSpeed);
        }
    }
}

//pushHandler detects when a player runs into a wall or obstacle � it watches the body-part trigger zones and checks
//  "was I moving fast enough when this trigger fired?"

//  rbImpactDet detects the physics collision itself � Unity's physics engine tells you the exact relative velocity at the
//   moment of impact, which is the "true" collision force.

//  ---
//  In practice:

//  -pushHandler is imprecise � trigger colliders don't give you collision force data, so it has to estimate speed by
//  sampling position every frame. It can fire even if you lightly brush a wall while moving fast, because it only checks
//  "were you moving fast" not "did the impact actually stop you hard."
//  - rbImpactDet is precise � collision.relativeVelocity is the actual physics impulse, so it only fires when something
//  genuinely hit hard. It's the "correct" way to detect impact damage.

//  ---
//  Why both exist:

//  The trigger-based approach (pushHandler / CollisionDetector) was the original system, designed around body-part
//  hitboxes. The rbImpactDet was added later as a comment in the file says � "body-part trigger colliders are inside the
//  main capsule and never reach obstacles (the capsule hits first)" � meaning the trigger body parts are buried inside
//  the character and physically can never touch a wall because the outer capsule collider blocks it first. So pushHandler
//   probably barely ever fires from environmental collisions; it's mainly useful for the IPushable / OnPushed() path
//  (when another player pushes you via raycast)