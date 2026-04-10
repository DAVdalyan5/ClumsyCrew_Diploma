using Assets.Scripts.Core.Character;
using Assets.Scripts.Core.Player.Character;
using Assets.Scripts.Core.Player.Mechanics.Pushing;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Assets.Scripts.Runtime.Helpers;
using Easy.MessageHub;
using HeistNSeek.Core.Player;
using HeistNSeek.Events;
using NaughtyAttributes;
using NaughtyAttributes.Test;
using R3;
using StarterAssets;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Assets.Scripts.Core.Player
{
    /// <summary>
    /// OBSOLETE? This was the original player controller that handled movement, balance, and ragdoll triggering.
    /// </summary>
    public class PlayerController : NetworkBehaviour
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private IMessageHub messageHub;

        private FirstPersonMovementHandler characterController;

        [Header("Balance Detection")]
        [Tooltip("Character's main transform (typically hips or root bone)")]
        [SerializeField] private Transform characterTransform;

        [SerializeField] private List<CollisionDetector> collisionDetectors;

        [SerializeField] private GameObject ragdollHierarchyPart;
        [SerializeField] private GameObject ragdollPositionRoot;


        private BalanceInfo currentBalanceInfo;
        public BalanceInfo CurrentBalanceInfo
        {
            get => currentBalanceInfo;
            set
            {
                currentBalanceInfo = value;
                characterController.IsBalanced = currentBalanceInfo.IsBalanced;
            }
        }

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            this.messageHub = messageHub;
        }

        private void Start()
        {
            InitializeController();

            if (!IsOwner)
            {
                // Non-owner clients only need to observe state changes
                // Collision detection and physics are handled by the owner
                Debug.Log("[PlayerController] Not owner, disabling collision detection.");
                DisableCollisionDetectors();
                return;
            }

            SetupOwnerBehavior();
        }

        private void InitializeController()
        {
            this.characterController = this.GetComponent<FirstPersonMovementHandler>();

            if (characterTransform == null)
            {
                Debug.LogError("[PlayerController] CharacterTransform or CharacterRigidbody not assigned!");
                return;
            }

            if (messageHub == null)
            {
                Debug.LogError("[PlayerController] IMessageHub not injected! Make sure PlayerController is registered in a LifetimeScope.");
                return;
            }

            this.CurrentBalanceInfo = new BalanceInfo { IsBalanced = true };
            this.characterController.IsBalanced = this.CurrentBalanceInfo.IsBalanced;
        }

        private void SetupOwnerBehavior()
        {
            var collisionDisposables = collisionDetectors.Select(detector => detector.Subscribe((collision, det) => HandleHighSpeedImpact(collision, det)));
            disposables.AddMany(collisionDisposables);
            this.messageHub.SubscribeSafe<ResetBalanceEvent>(this, ResetBalanceEventWrapper);
        }

        private void DisableCollisionDetectors()
        {
            // Disable collision detectors on non-owner clients
            if (collisionDetectors == null) return;

            foreach (var detector in collisionDetectors)
            {
                if (detector != null)
                {
                    detector.enabled = false;
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            disposables.Dispose();
        }

        [Rpc(SendTo.ClientsAndHost)] //TODO: idk why bit its(ragdoll) too strong on listening clients
        private void OnBalanceLostRpc(float impactSpeed)
        {
            currentBalanceInfo.IsBalanced = false;
            characterController.IsBalanced = false;

            if (!RagdollUtilities.IsRagdollEnabled(this.gameObject))
            {
                this.EnablePlayerRagdoll();

                // Only the owner should publish BalanceLostEvent to trigger item dropping
                // This prevents other players' droppers from receiving the event when
                // this RPC executes on hosts/other clients
                if (IsOwner)
                {
                    messageHub.Publish<BalanceLostEvent>(new BalanceLostEvent(impactSpeed));
                }
            }
        }

        private void OnBalanceRegained()
        {
            currentBalanceInfo.IsBalanced = true;
            characterController.IsBalanced = true;

            this.transform.position = ragdollPositionRoot.transform.position;

            // Disable ragdoll (enables animator and kinematic rigidbodies)
            this.DisablePlayerRagdoll();
        }

        private void ResetBalanceEventWrapper(ResetBalanceEvent args)
        {
            ResetPlayerBalanceRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ResetPlayerBalanceRpc()
        {
            this.CurrentBalanceInfo.IsBalanced = true;
            OnBalanceRegained();
        }

        private void HandleHighSpeedImpact(Collider collision, CollisionDetector detector)
        {
            // Use actual character speed - only trigger on genuine high-speed impacts
            float currentSpeed = characterController.CurrentSpeed;
            float effectiveSpeed = currentSpeed * detector.ImpactMultiplier;

            Debug.Log($"[PlayerController] Collision detected on {detector.name} with {collision.name} - Speed: {currentSpeed:F2}, Effective: {effectiveSpeed:F2}, Threshold: {detector.HighSpeedThreshold}", collision);

            // Only trigger balance loss on actual high-speed impacts, not incidental touches
            if (effectiveSpeed > detector.HighSpeedThreshold)
            {
                this.CurrentBalanceInfo.IsBalanced = false;
                Debug.Log($"[CharacterBalancer] Balance lost! Impact speed: {effectiveSpeed:F2}");
                OnBalanceLostRpc(effectiveSpeed);
            }
        }

        public void EnablePlayerRagdoll()
        {
            RagdollUtilities.ToggleRagdoll(ragdollHierarchyPart, true);
        }

        public void DisablePlayerRagdoll()
        {
            RagdollUtilities.ToggleRagdoll(ragdollHierarchyPart, false);
        }

        /// <summary>
        /// Called by the server when this player is pushed by another player.
        /// Triggers ragdoll on all clients via RPC.
        /// </summary>
        public void TriggerPushRagdoll(float force)
        {
            Debug.Log($"[PlayerController] TriggerPushRagdoll called with force: {force}");
            OnBalanceLostRpc(force);
        }
    }
}

