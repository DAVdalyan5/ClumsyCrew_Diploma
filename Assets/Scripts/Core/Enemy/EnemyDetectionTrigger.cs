using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.Enemy
{
    /// <summary>
    /// Trigger-based player detection for enemies. Fires on enter/exit instead of polling.
    /// Supports multiple co-op players; targets the closest when several are in range.
    /// Server-only: detection logic runs only on server.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnemyDetectionTrigger : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Layers that count as players. Typically includes Player layer.")]
        [SerializeField] private LayerMask playerLayerMask;
        [Tooltip("Fallback: also accept GameObjects with this tag (e.g. colliders on child objects).")]
        [SerializeField] private string playerTag = "Player";

        private EnemyControllerBase _controller;
        private readonly HashSet<Transform> _playersInRange = new HashSet<Transform>();
        private Collider _triggerCollider;

        private void Awake()
        {
            _controller = GetComponentInParent<EnemyControllerBase>();
            _triggerCollider = GetComponent<Collider>();

            if (playerLayerMask == 0)
            {
                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer >= 0)
                    playerLayerMask = 1 << playerLayer;
            }

            if (_controller == null)
            {
                Debug.LogError($"[EnemyDetectionTrigger] No EnemyControllerBase found in parent of {gameObject.name}.");
                enabled = false;
                return;
            }

            if (_triggerCollider != null && !_triggerCollider.isTrigger)
            {
                Debug.LogWarning($"[EnemyDetectionTrigger] Collider on {gameObject.name} is not a trigger. Setting isTrigger = true.");
                _triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServerOrStandalone())
                return;

            if (!IsPlayer(other))
                return;

            var playerRoot = GetPlayerRoot(other);
            if (playerRoot == null)
                return;

            _playersInRange.Add(playerRoot);
            UpdateCurrentTargetAndNotify();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServerOrStandalone())
                return;

            if (!IsPlayer(other))
                return;

            var playerRoot = GetPlayerRoot(other);
            if (playerRoot == null)
                return;

            _playersInRange.Remove(playerRoot);
            UpdateCurrentTargetAndNotify();
        }

        private bool IsServerOrStandalone()
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
                return true;
            return networkManager.IsServer;
        }

        private bool IsPlayer(Collider other)
        {
            if (playerLayerMask == 0)
                return other.CompareTag(playerTag);

            int layer = other.gameObject.layer;
            return ((1 << layer) & playerLayerMask) != 0 || other.CompareTag(playerTag);
        }

        private Transform GetPlayerRoot(Collider other)
        {
            var no = other.GetComponentInParent<NetworkObject>();
            if (no != null)
                return no.transform;

            if (other.CompareTag(playerTag))
                return other.transform;

            var root = other.transform.root;
            if (root.CompareTag(playerTag))
                return root;

            return other.transform;
        }

        private void UpdateCurrentTargetAndNotify()
        {
            _playersInRange.RemoveWhere(t => t == null);
            var newTarget = GetClosestPlayerInRange();

            if (newTarget != null)
            {
                _controller.OnPlayerDetected(newTarget);
            }
            else
            {
                _controller.OnAllPlayersLeft();
            }
        }

        private Transform GetClosestPlayerInRange()
        {
            if (_playersInRange.Count == 0)
                return null;

            Transform closest = null;
            float closestSqrDist = float.MaxValue;
            var myPos = transform.position;

            foreach (var t in _playersInRange)
            {
                if (t == null)
                    continue;

                float sqrDist = (t.position - myPos).sqrMagnitude;
                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    closest = t;
                }
            }

            return closest;
        }
    }
}
