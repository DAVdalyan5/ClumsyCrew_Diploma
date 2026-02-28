using System;
using Assets.Scripts.Core.Player.Character;
using Easy.MessageHub;
using HeistNSeek.Events;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Applies a "falling" camera effect when the local player goes ragdoll.
    /// Tilts the camera to simulate the disorienting feel of losing balance.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public class RagdollCameraEffect : NetworkBehaviour
    {
        [Header("References")]
        [Tooltip("Camera transform to apply the falling tilt (e.g. playerCam from PlayerMovementSettings). Auto-resolved if null.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Falling Effect")]
        [Tooltip("Pitch (look down) added over time while ragdoll, in degrees.")]
        [SerializeField] private float fallPitchAmount = 55f;
        [Tooltip("Time to reach full fall pitch.")]
        [SerializeField] private float fallPitchDuration = 1.2f;
        [Tooltip("Max roll (tilt) wobble in degrees during initial fall.")]
        [SerializeField] private float fallRollWobble = 8f;
        [Tooltip("Speed of the roll wobble oscillation.")]
        [SerializeField] private float wobbleSpeed = 5f;
        [Tooltip("How long the wobble lasts before fading out (stops to avoid motion sickness).")]
        [SerializeField] private float wobbleDuration = 0.5f;
        [Tooltip("Impact shake intensity when fall starts (0 = no shake).")]
        [SerializeField] private float impactShakeAmount = 0.4f;

        [Header("Recovery")]
        [Tooltip("Speed to reset camera when player gets up (higher = faster).")]
        [SerializeField] private float recoverySpeed = 12f;

        private IMessageHub _messageHub;
        private NetworkedCowsinsRagdollController _ragdollController;
        private Guid _balanceLostSubscription;

        private bool _isRagdollActive;
        private bool _isRecovering;
        private float _ragdollTime;
        private Quaternion _preFallLocalRotation;
        private Camera _cameraComponent;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            EnsureInjectedDependencies();
            ResolveReferences();

            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _ragdollController = GetComponent<NetworkedCowsinsRagdollController>();
            if (_ragdollController == null)
            {
                Debug.LogWarning("[RagdollCameraEffect] NetworkedCowsinsRagdollController not found. Effect disabled.");
                enabled = false;
                return;
            }

            _balanceLostSubscription = _messageHub.Subscribe<BalanceLostEvent>(OnBalanceLost);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (_messageHub != null && _balanceLostSubscription != Guid.Empty)
                _messageHub.Unsubscribe(_balanceLostSubscription);
        }

        private void EnsureInjectedDependencies()
        {
            if (_messageHub != null) return;

            LifetimeScope scope = FindAnyObjectByType<GameplayLifetimeScope>()
                ?? FindAnyObjectByType<LifetimeScope>();

            if (scope?.Container != null)
                scope.Container.InjectGameObject(gameObject);
        }

        private void ResolveReferences()
        {
            if (cameraTransform != null) return;

            var playerMovement = GetComponentInChildren<cowsins.PlayerMovement>(true);
            if (playerMovement != null && playerMovement.playerSettings.playerCam != null)
            {
                cameraTransform = playerMovement.playerSettings.playerCam;
                return;
            }

            var weaponController = GetComponentInChildren<cowsins.WeaponController>(true);
            if (weaponController?.MainCamera != null)
            {
                cameraTransform = weaponController.MainCamera.transform;
                return;
            }

            var cam = FindChildByName(transform, "Camera");
            if (cam != null)
            {
                _cameraComponent = cam.GetComponentInChildren<Camera>(true);
                cameraTransform = _cameraComponent != null ? _cameraComponent.transform : cam;
            }
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

        private void OnBalanceLost(BalanceLostEvent _)
        {
            if (!IsOwner || cameraTransform == null) return;

            _isRagdollActive = true;
            _ragdollTime = 0f;
            _preFallLocalRotation = cameraTransform.localRotation;

            TriggerImpactShake();
        }

        private void TriggerImpactShake()
        {
            if (impactShakeAmount <= 0f) return;

            var camFx = GetComponentInChildren<cowsins.CameraEffects>(true);
            if (camFx != null)
                camFx.Shake(impactShakeAmount, 20f, 0.9f, 20f);
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;

            if (_isRagdollActive)
            {
                UpdateRagdollEffect();
                return;
            }

            if (_isRecovering)
                ResetToNormal();
        }

        private void UpdateRagdollEffect()
        {
            if (_ragdollController.CurrentBalanceInfo.IsBalanced)
            {
                _isRagdollActive = false;
                _isRecovering = true;
                return;
            }

            _ragdollTime += Time.deltaTime;
            float t = Mathf.Clamp01(_ragdollTime / fallPitchDuration);

            float pitch = Mathf.Lerp(0f, -fallPitchAmount, t);
            float wobbleFade = Mathf.Max(0f, 1f - _ragdollTime / wobbleDuration);
            float wobble = Mathf.Sin(_ragdollTime * wobbleSpeed) * fallRollWobble * wobbleFade;

            Quaternion fallTilt = Quaternion.Euler(pitch, 0f, wobble);
            cameraTransform.localRotation = _preFallLocalRotation * fallTilt;
        }

        private void ResetToNormal()
        {
            if (Quaternion.Angle(cameraTransform.localRotation, _preFallLocalRotation) < 0.5f)
            {
                _isRecovering = false;
                cameraTransform.localRotation = _preFallLocalRotation;
                return;
            }

            cameraTransform.localRotation = Quaternion.Slerp(
                cameraTransform.localRotation,
                _preFallLocalRotation,
                recoverySpeed * Time.deltaTime
            );
        }
    }
}
