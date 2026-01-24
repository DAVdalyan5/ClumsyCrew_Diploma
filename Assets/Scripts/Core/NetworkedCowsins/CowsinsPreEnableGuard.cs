using System.Reflection;
using cowsins;
using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Prevents Cowsins components that assume Start()/Initialize() ran before OnEnable() from throwing during
    /// Netcode/VContainer instantiation flows.
    ///
    /// Strategy:
    /// - Awake(): disable problematic components so their OnEnable() won't execute yet.
    /// - Start(): if owner (or standalone), patch required private fields and re-enable them.
    /// - Non-owners: keep them disabled (owner-only behavior for input/camera/interaction).
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public class CowsinsPreEnableGuard : MonoBehaviour
    {
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private InteractManager _interact;
        private CameraEffects _cameraEffects;

        private void Awake()
        {
            _interact = GetComponent<InteractManager>();
            _cameraEffects = GetComponent<CameraEffects>();

            if (_interact != null) _interact.enabled = false;
            if (_cameraEffects != null) _cameraEffects.enabled = false;
        }

        private void Start()
        {
            if (!IsOwnerOrStandalone())
                return;

            // Patch dependencies before enabling
            PatchInteractManager();
            PatchCameraEffects();

            if (_interact != null) _interact.enabled = true;
            if (_cameraEffects != null) _cameraEffects.enabled = true;
        }

        private bool IsOwnerOrStandalone()
        {
            if (NetworkManager.Singleton == null) return true;

            var ownerBehaviour = GetComponentInParent<NetworkBehaviour>();
            if (ownerBehaviour == null) return false;
            return ownerBehaviour.IsOwner;
        }

        private void PatchInteractManager()
        {
            if (_interact == null) return;

            var weaponEventsProvider = GetComponent<IWeaponEventsProvider>();
            if (weaponEventsProvider != null) SetPrivateField(_interact, "weaponEvents", weaponEventsProvider);
        }

        private void PatchCameraEffects()
        {
            if (_cameraEffects == null) return;

            var weaponEventsProvider = GetComponent<IWeaponEventsProvider>();
            if (weaponEventsProvider != null) SetPrivateField(_cameraEffects, "weaponEvents", weaponEventsProvider);

            var player = GetComponent<IPlayerMovementStateProvider>();
            if (player != null) SetPrivateField(_cameraEffects, "player", player);

            var control = GetComponent<IPlayerControlProvider>();
            if (control != null) SetPrivateField(_cameraEffects, "playerControlProvider", control);

            var deps = GetComponent<PlayerDependencies>();
            var input = deps != null ? deps.InputManager : null;
            if (input == null)
            {
                input = GetComponentInChildren<InputManager>(true) ?? GetComponentInParent<InputManager>();
            }
            if (input != null) SetPrivateField(_cameraEffects, "inputManager", input);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var f = target.GetType().GetField(fieldName, PrivateInstance);
            if (f == null) return;
            f.SetValue(target, value);
        }
    }
}

