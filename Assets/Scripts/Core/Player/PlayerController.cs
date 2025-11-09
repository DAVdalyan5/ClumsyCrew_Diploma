using Assets.Scripts.Core.Character;
using Assets.Scripts.Core.Player.Character;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using R3;
using R3.Triggers;
using System.Threading.Tasks;
using UnityEngine;
using StarterAssets;

namespace Assets.Scripts.Core.Player
{
    //TODO:apply force during collision
    //disable movement
    //Stuck in the wall
    //add more colliders and subscirbe to them all.
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

        [SerializeField] private Collider triggerCollider;

        private CharacterBalancer balancer;
        private CharacterBalancer.BalanceInfo currentBalanceInfo;
        private FirstPersonController characterController;

        private IDisposable colliderSubscribe;

        private void Awake()
        {
            this.characterController = this.GetComponent<FirstPersonController>();

            if (characterTransform == null)
            {
                Debug.LogError("[PlayerController] CharacterTransform or CharacterRigidbody not assigned!");
                return;
            }

            this.balancer = new CharacterBalancer(characterTransform, forwardTiltThreshold, backwardTiltThreshold, highSpeedThreshold);
            this.colliderSubscribe = this.triggerCollider.OnTriggerEnterAsObservable()
                                    .Subscribe(collision =>
                                    {
                                        Debug.Log($"COLLISION HAPPENED {collision.name}", collision);
                                        balancer.OnCollision(characterController.CurrentSpeed);
                                    })
                                    .AddTo(this);
        }

        private void OnDestroy()
        {
            this.colliderSubscribe?.Dispose();
        }

        private void FixedUpdate()
        {
            if (balancer == null) return;

            currentBalanceInfo = balancer.CheckBalance();

            if (!currentBalanceInfo.IsBalanced && !RagdollUtilities.IsRagdollEnabled(this.gameObject))
            {
                this.EnablePlayerRagdoll();
            }
            else if (currentBalanceInfo.IsBalanced && RagdollUtilities.IsRagdollEnabled(this.gameObject))
            {
                this.DisablePlayerRagdoll();
            }

            // Debug output
            if (showDebugInfo)
            {
                Debug.Log($"[PlayerController] Balance Status:\n" +
                         $"  Balanced: {currentBalanceInfo.IsBalanced}\n" +
                         $"  Tipping Forward: {currentBalanceInfo.IsTippingForward}\n" +
                         $"  Tipping Backward: {currentBalanceInfo.IsTippingBackward}\n" +
                         $"  High Speed Impact: {currentBalanceInfo.HasHighSpeedImpact}\n" +
                         $"  Tilt Angle: {currentBalanceInfo.ForwardTiltAngle:F2}°");
            }
        }

        #region Test Area

        [Button("Enable Ragdoll")]
        public void EnablePlayerRagdoll()
        {
            this.gameObject.ToggleRagdoll(true);
        }

        [Button("Disable Ragdoll")]
        public void DisablePlayerRagdoll()
        {
            this.gameObject.ToggleRagdoll(false);
        }

        #endregion
    }
}
