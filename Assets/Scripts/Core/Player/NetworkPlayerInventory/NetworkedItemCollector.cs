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

        private IMessageHub _messageHub;

        private RaycastHit _lastHit;
        private bool _lastHitResult;
        private float _debugDrawTime;

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
                _messageHub.SubscribeSafe<InteractEvent>(this, _ => InteractActionPerform());
            }
        }

        /// <summary>
        /// Resolves IMessageHub from a LifetimeScope when injection hasn't run yet (e.g. client-spawned networked prefabs).
        /// </summary>
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

        private void InteractActionPerform()
        {
            if (!IsOwner) return;

            PerformRaycastInteraction();
        }

        private void PerformRaycastInteraction()
        {
            if (raycastCamera == null) return;

            Vector3 origin = raycastCamera.transform.position;
            Vector3 direction = raycastCamera.transform.forward;
            ulong localClientId = NetworkManager.Singleton.LocalClientId;

            // Layer-masked raycast for direct interactable hits
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
                    collectable.ForcePickup(localClientId);
                    return;
                }

                var targetInventory = hit.Value.collider.GetComponentInParent<NetworkedPlayerInventory>();
                if (targetInventory != null && targetInventory.OwnerClientId != localClientId)
                {
                    PerformPeek(targetInventory);
                    return;
                }

                // Hit an interactable-layer object but it's not an item or player — sphere fallback
                TryPickupNearbyItem(hit.Value.point, localClientId);
                return;
            }

            // No interactable hit — raycast against all geometry to find surface point (skip local player)
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

            // Nothing hit at all
            if (debugVisualization)
            {
                _lastHitResult = false;
                _debugDrawTime = Time.time + 2f;
            }
        }

        /// <summary>
        /// Skips hits on the local player's colliders so we can interact with objects/players in front of us.
        /// </summary>
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

            closest?.ForcePickup(localClientId);
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
