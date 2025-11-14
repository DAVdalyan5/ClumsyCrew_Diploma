using Assets.Scripts.Core.Character;
using Assets.Scripts.Core.Player.Character;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Assets.Scripts.Runtime.Helpers;
using Easy.MessageHub;
using HeistNSeek.Helpers;
using NaughtyAttributes;
using NUnit.Framework;
using R3;
using R3.Triggers;
using StarterAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro.EditorUtilities;
using UnityEngine;
using VContainer;

namespace Assets.Scripts.Core.Player
{
    //TODO: fix stucking in the wall, add a script s.t. each collsion detector can have its own threshold
    //TODO: could probably create a service to handle disposables. or inject a compositeDisposable when needed
    public class PlayerController : MonoBehaviour
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private IMessageHub messageHub;

        private CharacterBalancer balancer;
        private FirstPersonController characterController;

        [Header("Balance Detection")]
        [Tooltip("Character's main transform (typically hips or root bone)")]
        [SerializeField] private Transform characterTransform;

        [Tooltip("Velocity magnitude threshold for high-speed impacts")]
        [SerializeField] private float highSpeedThreshold = 5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        [SerializeField] private List<Collider> collisionDetectors;

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

        private void Awake()
        {
            this.characterController = this.GetComponent<FirstPersonController>();

            if (characterTransform == null)
            {
                Debug.LogError("[PlayerController] CharacterTransform or CharacterRigidbody not assigned!");
                return;
            }

            var collisionDisposables = collisionDetectors.Select(collider => collider.OnTriggerEnterAsObservable().Subscribe(collision => HandleHighSpeedImpact(collision)).AddTo(this));
            disposables.AddMany(collisionDisposables);

            this.characterController.BalanceResetAction += () => ResetPlayerBalance();
        }

        private void Start()
        {
            if (messageHub == null)
            {
                Debug.LogError("[PlayerController] IMessageHub not injected! Make sure PlayerController is registered in a LifetimeScope.");
                return;
            }

            this.balancer = new CharacterBalancer(messageHub, highSpeedThreshold);

            currentBalanceInfo.IsBalanced = true;
            characterController.IsBalanced = true;

            // Subscribe to balance events using SubscribeSafe extension
            messageHub.SubscribeSafe<BalanceLostEvent>(this, OnBalanceLost);
            messageHub.SubscribeSafe<BalanceRegainedEvent>(this, OnBalanceRegained);
        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        private void OnBalanceLost(BalanceLostEvent evt)
        {
            Debug.Log($"[PlayerController] Received BalanceLostEvent - Impact speed: {evt.ImpactSpeed}");
            currentBalanceInfo.IsBalanced = false;
            characterController.IsBalanced = false;

            if (!RagdollUtilities.IsRagdollEnabled(this.gameObject))
            {
                this.EnablePlayerRagdoll();
            }
        }

        private void OnBalanceRegained(BalanceRegainedEvent evt)
        {
            Debug.Log("[PlayerController] Received BalanceRegainedEvent");
            currentBalanceInfo.IsBalanced = true;
            characterController.IsBalanced = true;

            this.transform.position = ragdollPositionRoot.transform.position;

            // Disable ragdoll (enables animator and kinematic rigidbodies)
            this.DisablePlayerRagdoll();
        }

        private void ResetPlayerBalance()
        {
            Debug.Log("[PlayerController] Balance reset requested.");

            // This will publish BalanceRegainedEvent, which we'll handle in OnBalanceRegained
            balancer?.RegainBalance();
        }

        private void HandleHighSpeedImpact(Collider collision)
        {
            Debug.Log($"COLLISION HAPPENED {collision.name}", collision);
            balancer.OnCollision(characterController.CurrentSpeed);
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
