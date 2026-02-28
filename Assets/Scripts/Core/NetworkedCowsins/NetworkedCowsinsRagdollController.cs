using Assets.Scripts.Core.Character;
using Assets.Scripts.Core.Player.Character;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using cowsins;
using HeistNSeek.Core;
using HeistNSeek.Events;
using Easy.MessageHub;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Handles ragdoll state, balance, and RPCs for the networked Cowsins player.
    /// Designed for swappable model/ragdoll – assign different prefabs in Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedCowsinsRagdollController : NetworkBehaviour
    {
        [Header("Ragdoll (swappable)")]
        [Tooltip("Root GameObject of the ragdoll hierarchy (for ToggleRagdoll).")]
        [SerializeField] private GameObject ragdollHierarchyPart;
        [Tooltip("Transform for get-up teleport (typically hips/pelvis bone).")]
        [SerializeField] private Transform ragdollPositionRoot;
        [Tooltip("Which Rigidbody receives impact force. If null, uses first in hierarchy.")]
        [SerializeField] private Rigidbody forceApplicationBone;
        [Tooltip("Optional: for visibility swap with Cowsins PlayerGraphics.")]
        [SerializeField] private GameObject bodyModelRoot;

        [Header("Collision Detection")]
        [Tooltip("Collision detectors to disable for non-owners.")]
        [SerializeField] private List<CollisionDetector> collisionDetectors = new List<CollisionDetector>();

        private IMessageHub _messageHub;
        private NetworkedCowsinsPlayerController _playerController;
        private BalanceInfo _currentBalanceInfo;

        public BalanceInfo CurrentBalanceInfo => _currentBalanceInfo;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            EnsureInjectedDependencies();
            _playerController = GetComponent<NetworkedCowsinsPlayerController>();
            _currentBalanceInfo = new BalanceInfo { IsBalanced = true };

            if (!IsOwner)
            {
                DisableCollisionDetectors();
                HideCowsinsBodyWhenUsingRagdoll();
                return;
            }

            _messageHub.SubscribeSafe<ResetBalanceEvent>(this, OnResetBalanceRequested);
        }

        private void HideCowsinsBodyWhenUsingRagdoll()
        {
            if (ragdollHierarchyPart == null) return;
            var playerGraphics = FindChildByName(transform, "PlayerGraphics");
            if (playerGraphics != null)
                playerGraphics.gameObject.SetActive(false);
        }

        private static Transform FindChildByName(Transform parent, string name)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
                var found = FindChildByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void EnsureInjectedDependencies()
        {
            if (_messageHub != null) return;

            LifetimeScope scope = FindAnyObjectByType<GameplayLifetimeScope>();
            if (scope == null)
                scope = FindAnyObjectByType<LifetimeScope>();

            if (scope?.Container != null)
                scope.Container.InjectGameObject(gameObject);
        }

        private void OnResetBalanceRequested(ResetBalanceEvent _)
        {
            ResetPlayerBalanceServerRpc();
        }

        private void DisableCollisionDetectors()
        {
            if (collisionDetectors == null) return;
            foreach (var detector in collisionDetectors)
            {
                if (detector != null)
                    detector.enabled = false;
            }
        }

        /// <summary>
        /// Called by server when this player is pushed. Triggers ragdoll on all clients.
        /// </summary>
        public void TriggerPushRagdoll(Vector3 forceVector)
        {
            if (!IsServer) return;
            float impactSpeed = forceVector.magnitude;
            if (impactSpeed < 0.01f)
                forceVector = -transform.forward * 5f;
            OnBalanceLostClientRpc(impactSpeed, forceVector);
        }

        [ServerRpc]
        public void TriggerRagdollFromImpactServerRpc(Vector3 forceVector, float impactSpeed, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            var senderId = rpcParams.Receive.SenderClientId;
            if (senderId != OwnerClientId)
                return;
            OnBalanceLostClientRpc(impactSpeed, forceVector);
        }

        [ClientRpc]
        private void OnBalanceLostClientRpc(float impactSpeed, Vector3 forceVector)
        {
            _currentBalanceInfo.IsBalanced = false;

            if (ragdollHierarchyPart == null)
            {
                Debug.LogWarning("[NetworkedCowsinsRagdollController] ragdollHierarchyPart not assigned.");
                return;
            }

            if (!RagdollUtilities.IsRagdollEnabled(ragdollHierarchyPart))
            {
                EnableRagdoll();
                RagdollUtilities.ApplyForceToRagdoll(ragdollHierarchyPart, forceVector, ForceMode.Impulse, forceApplicationBone);

                if (IsOwner && _messageHub != null)
                    _messageHub.Publish(new BalanceLostEvent(impactSpeed));
            }
        }

        [ServerRpc]
        private void ResetPlayerBalanceServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
                return;
            ResetPlayerBalanceClientRpc();
        }

        [ClientRpc]
        private void ResetPlayerBalanceClientRpc()
        {
            _currentBalanceInfo.IsBalanced = true;
            OnBalanceRegained();
        }

        private void EnableRagdoll()
        {
            DisableMovementAndRigidbody();
            RagdollUtilities.ToggleRagdoll(ragdollHierarchyPart, true);
        }

        private void OnBalanceRegained()
        {
            if (ragdollPositionRoot != null)
                transform.position = ragdollPositionRoot.position;

            RagdollUtilities.ToggleRagdoll(ragdollHierarchyPart, false);
            ReEnableMovementAndRigidbody();
        }

        private void DisableMovementAndRigidbody()
        {
            if (_playerController != null)
            {
                var pm = _playerController.GetPlayerMovement();
                if (pm != null)
                    pm.enabled = false;

                if (IsOwner)
                    pm?.GetComponent<PlayerControl>()?.LoseControl();
            }

            var moving = _playerController != null ? _playerController.GetMovingTransform() : transform;
            var rb = moving != null ? moving.GetComponent<Rigidbody>() : null;
            if (rb != null)
                rb.isKinematic = true;
        }

        private void ReEnableMovementAndRigidbody()
        {
            if (!IsOwner) return;

            if (_playerController != null)
            {
                var pm = _playerController.GetPlayerMovement();
                if (pm != null)
                {
                    pm.enabled = true;
                    pm.GetComponent<PlayerControl>()?.CheckIfCanGrantControl();
                }
            }

            var moving = _playerController != null ? _playerController.GetMovingTransform() : transform;
            var rb = moving != null ? moving.GetComponent<Rigidbody>() : null;
            if (rb != null)
                rb.isKinematic = false;
        }
    }
}
