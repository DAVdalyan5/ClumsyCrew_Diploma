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

        private NetworkedCowsinsPlayerController _controller;
        private NetworkedCowsinsRagdollController _ragdollController;
        private readonly List<System.IDisposable> _subscriptions = new List<System.IDisposable>();
        private float _lastStumbleTime;
        private Vector3 _lastMovingPosition;
        private bool _hasLastMovingPosition;
        private float _estimatedPlanarSpeed;

        private void Start()
        {
            _controller = GetComponentInParent<NetworkedCowsinsPlayerController>();
            _ragdollController = GetComponentInParent<NetworkedCowsinsRagdollController>();
            if (_controller == null || !_controller.IsOwner)
                return;

            var moving = _controller.GetMovingTransform();
            if (moving != null)
            {
                _lastMovingPosition = moving.position;
                _hasLastMovingPosition = true;
            }

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
        /// The logic: compute the direction from the player's center to the other collider's center — if that direction dot
        /// Vector3.down > 0.5f (more than ~60° pointing downward), it's underneath the player and treated as ground, so the
        /// impact is skipped.
        /// The 0.5f threshold means only things that are clearly below you are ignored.A wall to the side will have a near-zero
        /// or negative dot product with Vector3.down and will still trigger ragdoll normally.
        /// </summary>
        private static bool IsBelowPlayer(Collider other, Transform playerTransform)
        {
            //this not used, but couuld be if needed.
            Vector3 toOther = other.bounds.center - playerTransform.position;
            return Vector3.Dot(toOther.normalized, Vector3.down) > 0.5f;
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

            var rb = GetMovingRigidbody();
            if (rb == null) return;

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