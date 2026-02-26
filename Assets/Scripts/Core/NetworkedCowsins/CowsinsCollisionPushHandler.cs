using Assets.Scripts.Core.Player.Character;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Subscribes to CollisionDetector(s) on the Cowsins player and applies a stumble force
    /// when high-speed impact is detected (Rigidbody-based, no ragdoll).
    /// Only active for the owning client; applies force via server RPC.
    /// </summary>
    public class CowsinsCollisionPushHandler : MonoBehaviour
    {
        [Header("Collision Detection")]
        [Tooltip("Collision detectors to subscribe to (e.g. torso, head).")]
        [SerializeField] private List<CollisionDetector> collisionDetectors = new List<CollisionDetector>();

        [Header("Stumble")]
        [Tooltip("Scale applied to the stumble impulse magnitude when threshold is exceeded.")]
        [SerializeField] private float stumbleImpulseScale = 1f;
        [Tooltip("Minimum seconds between stumble force applications.")]
        [SerializeField] private float stumbleCooldownSeconds = 0.2f;

        private NetworkedCowsinsPlayerController _controller;
        private readonly List<System.IDisposable> _subscriptions = new List<System.IDisposable>();
        private float _lastStumbleTime;
        private Vector3 _lastMovingPosition;
        private bool _hasLastMovingPosition;
        private float _estimatedPlanarSpeed;

        private void Start()
        {
            _controller = GetComponentInParent<NetworkedCowsinsPlayerController>();
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

        private void OnCollisionDetected(Collider other, CollisionDetector detector)
        {
            if (other == null || detector == null)
                return;
            if (_controller == null)
                return;
            if (other.transform.IsChildOf(_controller.transform))
                return; // Ignore own colliders; they can spam trigger callbacks.

            var rb = GetMovingRigidbody();
            if (rb == null) return;

            var planarVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float sampledPlanarSpeed = Mathf.Max(planarVelocity.magnitude, _estimatedPlanarSpeed);
            float effectiveSpeed = sampledPlanarSpeed * detector.ImpactMultiplier;

            if (effectiveSpeed <= detector.HighSpeedThreshold)
                return;
            if (Time.unscaledTime - _lastStumbleTime < stumbleCooldownSeconds)
                return;

            Vector3 direction = planarVelocity.sqrMagnitude > 0.01f
                ? -planarVelocity.normalized
                : -_controller.transform.forward;
            float magnitude = (effectiveSpeed - detector.HighSpeedThreshold) * stumbleImpulseScale;
            Vector3 forceVector = direction * magnitude;
            _lastStumbleTime = Time.unscaledTime;
            Debug.Log("WE LOST THE FUCKIN BALANCE !!!!");
            // TODO: Activate networked ragdoll here and apply forceVector to ragdoll rigidbodies.
        }

        private Rigidbody GetMovingRigidbody()
        {
            if (_controller == null) return null;
            var moving = _controller.GetMovingTransform();
            return moving != null ? moving.GetComponent<Rigidbody>() : null;
        }
    }
}
