using Assets.Scripts.Core.Character;
using Assets.Scripts.Core.Player.Character;
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

namespace Assets.Scripts.Core.Player
{
    //TODO: disable movement, fix stucking in the wall, add a script s.t. each collsion detector can have its own threshold
    public class PlayerController : MonoBehaviour
    {
        [Header("Balance Detection")]
        [Tooltip("Character's main transform (typically hips or root bone)")]
        [SerializeField] private Transform characterTransform;

        [Tooltip("Maximum forward tilt angle before considered unbalanced (degrees)")]
        [SerializeField] private float forwardTiltThreshold = 45f;

        [Tooltip("Maximum backward tilt angle before considered unbalanced (degrees)")]
        [SerializeField] private float backwardTiltThreshold = 30f;

        [Tooltip("Velocity magnitude threshold for high-speed impacts")]
        [SerializeField] private float highSpeedThreshold = 5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        [SerializeField] private List<Collider> collisionDetectors;
        private List<IDisposable> colliderSubscribes = new();

        [SerializeField] private GameObject ragdollHierarchyPart;

        private CharacterBalancer balancer;
        private BalanceInfo currentBalanceInfo;
        private FirstPersonController characterController;


        private void Awake()
        {
            this.characterController = this.GetComponent<FirstPersonController>();

            if (characterTransform == null)
            {
                Debug.LogError("[PlayerController] CharacterTransform or CharacterRigidbody not assigned!");
                return;
            }

            this.balancer = new CharacterBalancer(highSpeedThreshold);

            this.colliderSubscribes.AddRange(collisionDetectors.Select(collider => collider.OnTriggerEnterAsObservable().Subscribe(collision => HandleHighSpeedImpact(collision)).AddTo(this)));

            this.characterController.BalanceResetAction += () => ResetPlayerBalance();
        }

        private void OnDestroy()
        {
            this.colliderSubscribes?.ForEach(c => c?.Dispose());
        }

        private void FixedUpdate()
        {
            if (balancer == null) return;

            currentBalanceInfo = balancer.CheckBalance();

            if (!currentBalanceInfo.IsBalanced && !RagdollUtilities.IsRagdollEnabled(this.gameObject))
            {
                this.EnablePlayerRagdoll();
            }
        }

        private void ResetPlayerBalance()
        {
            currentBalanceInfo.IsBalanced = true;
            Debug.Log("[PlayerController] Balance reset requested.");
            this.DisablePlayerRagdoll();
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
