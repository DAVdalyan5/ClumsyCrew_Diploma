using Assets.Scripts.Core.Character;
using Assets.Scripts.Core.Player.Character;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Assets.Scripts.Runtime.Helpers;
using Easy.MessageHub;
using HeistNSeek.Core.Player;
using HeistNSeek.Events;
using NaughtyAttributes;
using R3;
using StarterAssets;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;

namespace Assets.Scripts.Core.Player
{
    //TODO: fix stucking in the wall
    public class PlayerController : MonoBehaviour
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

            var collisionDisposables = collisionDetectors.Select(detector => detector.Subscribe((collision, det) => HandleHighSpeedImpact(collision, det)));

            disposables.AddMany(collisionDisposables);

            this.messageHub.SubscribeSafe<ResetBalanceEvent>(this, ResetPlayerBalance);
        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        private void OnBalanceLost(float impactSpeed)
        {
            Debug.Log($"[PlayerController] Received BalanceLostEvent - Impact speed: {impactSpeed}");
            currentBalanceInfo.IsBalanced = false;
            characterController.IsBalanced = false;

            if (!RagdollUtilities.IsRagdollEnabled(this.gameObject))
            {
                this.EnablePlayerRagdoll();
                messageHub.Publish<BalanceLostEvent>(new BalanceLostEvent(impactSpeed));
            }
        }

        private void OnBalanceRegained()
        {
            Debug.Log("[PlayerController] Received BalanceRegainedEvent");
            currentBalanceInfo.IsBalanced = true;
            characterController.IsBalanced = true;

            this.transform.position = ragdollPositionRoot.transform.position;

            // Disable ragdoll (enables animator and kinematic rigidbodies)
            this.DisablePlayerRagdoll();
        }

        private void ResetPlayerBalance(ResetBalanceEvent args)
        {
            Debug.Log("[PlayerController] Balance reset requested.");

            this.CurrentBalanceInfo.IsBalanced = true;
            OnBalanceRegained();
        }

        private void HandleHighSpeedImpact(Collider collision, CollisionDetector detector)
        {
            float currentSpeed = characterController.CurrentSpeed;
            float effectiveSpeed = currentSpeed * detector.ImpactMultiplier;

            Debug.Log($"[PlayerController] Collision detected on {detector.name} with {collision.name} - Speed: {currentSpeed}, Effective: {effectiveSpeed}, Threshold: {detector.HighSpeedThreshold}", collision);

            if (effectiveSpeed > detector.HighSpeedThreshold)
            {
                this.CurrentBalanceInfo.IsBalanced = false;
                Debug.Log($"[CharacterBalancer] Balance lost! Impact speed: {effectiveSpeed}");
                OnBalanceLost(effectiveSpeed);
            }
        }

        #region Test Area

        [Button("Enable Ragdoll")]
        public void EnablePlayerRagdoll()
        {
            RagdollUtilities.ToggleRagdoll(ragdollHierarchyPart, true);
        }

        [Button("Disable Ragdoll")]
        public void DisablePlayerRagdoll()
        {
            RagdollUtilities.ToggleRagdoll(ragdollHierarchyPart, false);
        }

        #endregion
    }
}
