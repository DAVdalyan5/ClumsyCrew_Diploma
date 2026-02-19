using System.Text;
using Assets.Scripts.Events;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Easy.MessageHub;
using HeistNSeek.Core.Inventory.NetworkedInventory;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Assets.Scripts.Core.Player.NetworkPlayerInventory
{
    public class NetworkedItemCollector : NetworkBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private float interactMaxDistance = 4f;
        [SerializeField] private LayerMask interactLayerMask;
        [SerializeField] private bool debugVisualization = true;

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

            _messageHub.SubscribeSafe<InteractEvent>(this, _ => InteractActionPerform());
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

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, interactMaxDistance, interactLayerMask))
            {
                if (debugVisualization)
                {
                    _lastHitResult = false;
                    _debugDrawTime = Time.time + 2f;
                }
                return;
            }

            if (debugVisualization)
            {
                _lastHit = hit;
                _lastHitResult = true;
                _debugDrawTime = Time.time + 2f;
            }

            ulong localClientId = NetworkManager.Singleton.LocalClientId;

            var collectable = hit.collider.GetComponentInParent<ICollectable>();
            if (collectable != null)
            {
                collectable.ForcePickup(localClientId);
                return;
            }

            var targetInventory = hit.collider.GetComponentInParent<NetworkedPlayerInventory>();
            if (targetInventory != null && targetInventory.OwnerClientId != localClientId)
            {
                PerformPeek(targetInventory);
            }
        }

        private void PerformPeek(NetworkedPlayerInventory targetInventory)
        {
            if (targetInventory.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                return;

            var sb = new StringBuilder();
            sb.Append($"[Peek] Player {targetInventory.OwnerClientId} inventory: ");

            var items = targetInventory.GetAllItems();
            int count = 0;
            foreach (var (itemId, amount) in items)
            {
                if (count > 0) sb.Append(", ");
                sb.Append($"{itemId} x{amount}");
                count++;
            }

            if (count == 0)
            {
                sb.Append("(empty)");
            }

            Debug.Log(sb.ToString());
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
