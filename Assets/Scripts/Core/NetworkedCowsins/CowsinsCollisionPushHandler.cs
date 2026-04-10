using Assets.Scripts.Core.Player.Character;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Subscribes to CollisionDetector(s) on the Cowsins player and triggers ragdoll
    /// when high-speed impact is detected.
    /// Only active for the owning client; applies via server RPC.
    /// </summary>
    public class CowsinsCollisionPushHandler : MonoBehaviour
    {
        [Header("Collision Detection")]
        [Tooltip("Collision detectors to subscribe to (e.g. torso, head).")]
        [SerializeField] private List<CollisionDetector> collisionDetectors = new List<CollisionDetector>();

        [Header("Ragdoll Impact")]
        [Tooltip("Scale applied to the force magnitude when threshold is exceeded.")]
        [SerializeField] private float impactForceScale = 1f;
        [Tooltip("Minimum seconds between ragdoll triggers.")]
        [SerializeField] private float impactCooldownSeconds = 0.2f;
        [Tooltip("Time after landing during which ragdoll triggers are ignored.")]
        [SerializeField] private float postLandingGracePeriod = 0.3f;
        [Tooltip("Ignore collisions from objects below the player (ground-like).")]
        [SerializeField] private bool ignoreCollisionsBelowPlayer = true;
        [Tooltip("Angle threshold (degrees) for 'below player' check. Higher = more lenient.")]
        [SerializeField] private float belowPlayerAngleThreshold = 45f;

        private NetworkedCowsinsPlayerController _controller;
        private NetworkedCowsinsRagdollController _ragdollController;
        private cowsins.PlayerMovement _playerMovement;
        private readonly List<System.IDisposable> _subscriptions = new List<System.IDisposable>();
        private float _lastStumbleTime;
        private Vector3 _lastMovingPosition;
        private bool _hasLastMovingPosition;
        private float _estimatedPlanarSpeed;
        private float _lastLandingTime;
        private bool _wasGroundedLastFrame;

        private void Start()
        {
            _controller = GetComponentInParent<NetworkedCowsinsPlayerController>();
            _ragdollController = GetComponentInParent<NetworkedCowsinsRagdollController>();
            if (_controller == null || !_controller.IsOwner)
                return;

            _playerMovement = _controller.GetPlayerMovement();

            var moving = _controller.GetMovingTransform();
            if (moving != null)
            {
                _lastMovingPosition = moving.position;
                _hasLastMovingPosition = true;
            }

            _wasGroundedLastFrame = _playerMovement != null && _playerMovement.Grounded;
            _lastLandingTime = -postLandingGracePeriod; // Allow immediate triggers at start

            if (collisionDetectors == null || collisionDetectors.Count == 0)
                return;

            foreach (var detector in collisionDetectors)
            {
                if (detector == null) continue;
                var sub = detector.Subscribe(OnCollisionDetected);
                if (sub != null)
                    _subscriptions.Add(sub);
            }
        }

        private void Update()
        {
            if (_controller == null || !_controller.IsOwner)
                return;

            var moving = _controller.GetMovingTransform();
            if (moving == null)
                return;

            // Track landing events for grace period
            if (_playerMovement != null)
            {
                bool isGroundedNow = _playerMovement.Grounded;
                if (isGroundedNow && !_wasGroundedLastFrame)
                {
                    // Just landed
                    _lastLandingTime = Time.unscaledTime;
                }
                _wasGroundedLastFrame = isGroundedNow;
            }

            if (!_hasLastMovingPosition)
            {
                _lastMovingPosition = moving.position;
                _hasLastMovingPosition = true;
                return;
            }

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 frameDelta = moving.position - _lastMovingPosition;
            Vector3 planarDelta = new Vector3(frameDelta.x, 0f, frameDelta.z);
            _estimatedPlanarSpeed = planarDelta.magnitude / dt;
            _lastMovingPosition = moving.position;
        }

        private void OnDestroy()
        {
            foreach (var sub in _subscriptions)
                sub?.Dispose();
            _subscriptions.Clear();
        }

        /// <summary>
        /// Checks if a collider is below the player (ground-like).
        /// Uses angle threshold to determine if the collision is from below.
        /// </summary>
        private bool IsBelowPlayer(Collider other, Transform playerTransform)
        {
            Vector3 toOther = other.bounds.center - playerTransform.position;
            // Convert angle threshold to dot product threshold
            // 45 degrees = cos(45) ≈ 0.707
            // Higher angle = lower dot threshold = more lenient
            float dotThreshold = Mathf.Cos(belowPlayerAngleThreshold * Mathf.Deg2Rad);
            return Vector3.Dot(toOther.normalized, Vector3.down) > dotThreshold;
        }

        private void OnCollisionDetected(Collider other, CollisionDetector detector)
        {
            if (other == null || detector == null)
                return;
            if (_controller == null)
                return;
            if (other.transform.IsChildOf(_controller.transform))
                return; // Ignore own colliders; they can spam trigger callbacks.
            if (other.CompareTag("Ground"))
                return;

            // Skip if player is currently grounded and running normally
            // Only trigger ragdoll when airborne or during actual high-speed wall impacts
            if (_playerMovement != null && _playerMovement.Grounded)
                return;

            // Skip collisions from objects below the player (ground-like impacts)
            if (ignoreCollisionsBelowPlayer && IsBelowPlayer(other, _controller.transform))
                return;

            // Skip during post-landing grace period (physics settling after a jump/fall)
            if (Time.unscaledTime - _lastLandingTime < postLandingGracePeriod)
                return;

            var rb = GetMovingRigidbody();
            if (rb == null) return;

            // Only use horizontal (planar) velocity for impact calculation
            // This prevents vertical landing velocity from triggering false ragdolls
            var planarVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float sampledPlanarSpeed = Mathf.Max(planarVelocity.magnitude, _estimatedPlanarSpeed);
            float effectiveSpeed = sampledPlanarSpeed * detector.ImpactMultiplier;

            if (effectiveSpeed <= detector.HighSpeedThreshold)
                return;
            if (Time.unscaledTime - _lastStumbleTime < impactCooldownSeconds)
                return;

            Vector3 direction = planarVelocity.sqrMagnitude > 0.01f
                ? -planarVelocity.normalized
                : -_controller.transform.forward;
            float magnitude = (effectiveSpeed - detector.HighSpeedThreshold) * impactForceScale;
            Vector3 forceVector = direction * magnitude;
            _lastStumbleTime = Time.unscaledTime;

            Debug.Log($"[CowsinsCollisionPushHandler] Ragdoll triggered - Collider: {other.name}, Speed: {effectiveSpeed:F2}, Threshold: {detector.HighSpeedThreshold}");

            if (_ragdollController != null)
                _ragdollController.TriggerRagdollFromImpactServerRpc(forceVector, effectiveSpeed);
        }

        private Rigidbody GetMovingRigidbody()
        {
            if (_controller == null) return null;
            var moving = _controller.GetMovingTransform();
            return moving != null ? moving.GetComponent<Rigidbody>() : null;
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