using Assets.Scripts.Events;
using HeistNSeek.Events;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Easy.MessageHub;
using HeistNSeek.Core.Inventory.NetworkedInventory;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Assets.Scripts.Core.Player.NetworkPlayerInventory
{
    public class NetworkedItemCollector : NetworkBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private float interactMaxDistance = 4f;
        [SerializeField] private LayerMask interactLayerMask;
        [SerializeField] private bool debugVisualization = true;

        [Header("Sphere Pickup Settings")]
        [SerializeField] private float pickupSphereRadius = 0.5f;
        [SerializeField] private LayerMask itemLayerMask;

        [Header("Hold to Pickup")]
        [SerializeField] private float holdDuration = 1.5f;

        private IMessageHub _messageHub;

        private RaycastHit _lastHit;
        private bool _lastHitResult;
        private float _debugDrawTime;

        private ICollectable _pendingTarget;
        private float _holdTimer;
        private bool _isHolding;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        private void Awake()
        {
            if (interactLayerMask == 0)
            {
                interactLayerMask = LayerMask.GetMask("Object", "Player");
            }

            EnsureMessageHubResolved();
            if (_messageHub != null)
            {
                _messageHub.SubscribeSafe<InteractEvent>(this, _ => OnInteractStarted());
                _messageHub.SubscribeSafe<InteractReleasedEvent>(this, _ => OnInteractReleased());
            }
        }

        private void EnsureMessageHubResolved()
        {
            if (_messageHub != null) return;

            LifetimeScope scope = FindAnyObjectByType<global::BootstrapLifetimeScope>();
            if (scope == null)
                scope = FindAnyObjectByType<LifetimeScope>();
            if (scope != null)
            {
                _messageHub = scope.Container.Resolve<IMessageHub>();
            }
        }

        private void Update()
        {
            if (!IsOwner || !_isHolding) return;

            _holdTimer += Time.deltaTime;
            if (_holdTimer >= holdDuration)
            {
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                _pendingTarget.ForcePickup(localClientId);
                CancelHold();
            }
        }

        private void OnInteractStarted()
        {
            if (!IsOwner) return;

            PerformRaycastInteraction();
        }

        private void OnInteractReleased()
        {
            if (!IsOwner) return;
            if (_isHolding)
                CancelHold();
        }

        private void CancelHold()
        {
            _isHolding = false;
            _holdTimer = 0f;
            _pendingTarget = null;
            _messageHub?.Publish(new InteractHoldCanceledEvent());
        }

        private void PerformRaycastInteraction()
        {
            if (raycastCamera == null) return;

            Vector3 origin = raycastCamera.transform.position;
            Vector3 direction = raycastCamera.transform.forward;
            ulong localClientId = NetworkManager.Singleton.LocalClientId;

            var hits = Physics.RaycastAll(origin, direction, interactMaxDistance, interactLayerMask);
            var hit = GetFirstValidHit(hits, localClientId);

            if (hit.HasValue)
            {
                if (debugVisualization)
                {
                    _lastHit = hit.Value;
                    _lastHitResult = true;
                    _debugDrawTime = Time.time + 2f;
                }

                var collectable = hit.Value.collider.GetComponentInParent<ICollectable>();
                if (collectable != null)
                {
                    StartHold(collectable);
                    return;
                }

                var targetInventory = hit.Value.collider.GetComponentInParent<NetworkedPlayerInventory>();
                if (targetInventory != null && targetInventory.OwnerClientId != localClientId)
                {
                    PerformPeek(targetInventory);
                    return;
                }

                TryPickupNearbyItem(hit.Value.point, localClientId);
                return;
            }

            var surfaceHits = Physics.RaycastAll(origin, direction, interactMaxDistance);
            var surfaceHit = GetFirstValidHit(surfaceHits, localClientId);
            if (surfaceHit.HasValue)
            {
                if (debugVisualization)
                {
                    _lastHit = surfaceHit.Value;
                    _lastHitResult = true;
                    _debugDrawTime = Time.time + 2f;
                }

                TryPickupNearbyItem(surfaceHit.Value.point, localClientId);
                return;
            }

            if (debugVisualization)
            {
                _lastHitResult = false;
                _debugDrawTime = Time.time + 2f;
            }
        }

        private void StartHold(ICollectable target)
        {
            _pendingTarget = target;
            _holdTimer = 0f;
            _isHolding = true;
            _messageHub?.Publish(new InteractHoldStartEvent(holdDuration));
        }

        private RaycastHit? GetFirstValidHit(RaycastHit[] hits, ulong localClientId)
        {
            foreach (var h in hits)
            {
                var inventory = h.collider.GetComponentInParent<NetworkedPlayerInventory>();
                if (inventory != null && inventory.OwnerClientId == localClientId)
                    continue;

                return h;
            }
            return null;
        }

        private void TryPickupNearbyItem(Vector3 center, ulong localClientId)
        {
            LayerMask mask = itemLayerMask != 0 ? itemLayerMask : interactLayerMask;
            var colliders = Physics.OverlapSphere(center, pickupSphereRadius, mask);

            ICollectable closest = null;
            float closestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                var item = col.GetComponentInParent<ICollectable>();
                if (item == null) continue;

                float dist = Vector3.Distance(center, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = item;
                }
            }

            if (closest != null)
                StartHold(closest);
        }

        private void PerformPeek(NetworkedPlayerInventory targetInventory)
        {
            if (targetInventory.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                return;

            _messageHub.Publish(new PeekEvent(targetInventory));
        }

        private void OnDrawGizmos()
        {
            if (!debugVisualization || raycastCamera == null) return;

            Vector3 origin = raycastCamera.transform.position;
            Vector3 direction = raycastCamera.transform.forward;

            if (Time.time < _debugDrawTime && _lastHitResult)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, _lastHit.point);
                Gizmos.DrawWireSphere(_lastHit.point, 0.1f);

                Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
                Gizmos.DrawWireSphere(_lastHit.point, pickupSphereRadius);
            }
            else if (Time.time < _debugDrawTime)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, origin + direction * interactMaxDistance);
            }
            else
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawLine(origin, origin + direction * interactMaxDistance);
            }
        }
    }
}
