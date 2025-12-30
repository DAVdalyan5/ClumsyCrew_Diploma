using Assets.Scripts.Core.Player;
using Assets.Scripts.Events;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Easy.MessageHub;
using HeistNSeek.Core.Inventory.NetworkedInventory;
using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Assets.Scripts.Core.Player.NetworkPlayerInventory
{
    public class NetworkedItemCollector : NetworkBehaviour
    {
        [SerializeField] private KeyCode pickupKey = KeyCode.E;
        [SerializeField] private float pickupRadius = 1.5f;
        [SerializeField] private float pickupDistance = 1.5f;
        [SerializeField] private bool debugVisualization = true;

        private IMessageHub _messageHub;

        private Transform _interactOrigin;
        private Camera _playerCamera;
        private LayerMask _pickupLayerMask;

        private Vector3 _lastSphereCenter;
        private bool _lastHitResult;
        private float _debugDrawTime;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        private void Awake()
        {
            _interactOrigin = GetComponentInChildren<InteractionPositionMarker>().transform;
            _playerCamera = GetComponentInChildren<Camera>();

            // Create layer mask that ignores Player and CollisionDetect layers
            int layersToIgnore = LayerMask.GetMask("Player", "CollisionDetect");
            _pickupLayerMask = ~layersToIgnore;

            _messageHub.SubscribeSafe<InteractEvent>(this, _ => InteractActionPerform());
        }

        private void InteractActionPerform()
        {
            if (!IsOwner) return;

            PerformSpherePickup();
        }

        private Vector3 GetSphereCenter()
        {
            Vector3 origin = _interactOrigin.position;
            Vector3 lookDirection = _playerCamera != null
                ? _playerCamera.transform.forward
                : transform.forward;

            return origin + lookDirection * pickupDistance;
        }

        private void PerformSpherePickup()
        {
            Vector3 sphereCenter = GetSphereCenter();

            // Find all colliders in the pickup sphere, ignoring Player and CollisionDetect layers
            Collider[] hitColliders = Physics.OverlapSphere(sphereCenter, pickupRadius, _pickupLayerMask);

            ICollectable closestCollectable = null;
            float closestDistance = float.MaxValue;

            foreach (var collider in hitColliders)
            {
                var collectable = collider.GetComponentInParent<ICollectable>();
                if (collectable != null)
                {
                    float distance = Vector3.Distance(sphereCenter, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestCollectable = collectable;
                    }
                }
            }

            if (debugVisualization)
            {
                _lastSphereCenter = sphereCenter;
                _lastHitResult = closestCollectable != null;
                _debugDrawTime = Time.time + 2f;
            }

            if (closestCollectable != null)
            {
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                closestCollectable.ForcePickup(localClientId);
            }
        }

        private void OnDrawGizmos()
        {
            if (!debugVisualization) return;

            Vector3 spherePos;
            if (Time.time < _debugDrawTime)
            {
                // Show last pickup attempt position
                spherePos = _lastSphereCenter;
                Gizmos.color = _lastHitResult ? Color.green : Color.red;
            }
            else if (_interactOrigin != null && _playerCamera != null)
            {
                // Show current pickup area in front of player
                spherePos = GetSphereCenter();
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            }
            else
            {
                return;
            }

            Gizmos.DrawWireSphere(spherePos, pickupRadius);
        }
    }
}
