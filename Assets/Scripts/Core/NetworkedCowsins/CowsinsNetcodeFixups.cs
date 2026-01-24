using System.Reflection;
using cowsins;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Fixes Cowsins scripts that incorrectly assume Start() ran before OnEnable().
    /// We patch required private fields in Awake() so OnEnable() won't NRE.
    /// </summary>
    [DisallowMultipleComponent]
    public class CowsinsNetcodeFixups : MonoBehaviour
    {
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private void Awake()
        {
            PatchInteractManager();
            PatchCameraEffects();
        }

        private void PatchInteractManager()
        {
            var interact = GetComponent<InteractManager>();
            if (interact == null) return;

            // InteractManager.OnEnable uses private field `weaponEvents` before Start() assigns it.
            var weaponEventsProvider = GetComponent<IWeaponEventsProvider>();
            if (weaponEventsProvider == null) return;

            SetPrivateField(interact, "weaponEvents", weaponEventsProvider);
        }

        private void PatchCameraEffects()
        {
            var camFx = GetComponent<CameraEffects>();
            if (camFx == null) return;

            // CameraEffects.OnEnable uses `weaponEvents` before Initialize() if PlayerDependencies refs are missing.
            var weaponEventsProvider = GetComponent<IWeaponEventsProvider>();
            if (weaponEventsProvider != null) SetPrivateField(camFx, "weaponEvents", weaponEventsProvider);

            var player = GetComponent<IPlayerMovementStateProvider>();
            if (player != null) SetPrivateField(camFx, "player", player);

            var control = GetComponent<IPlayerControlProvider>();
            if (control != null) SetPrivateField(camFx, "playerControlProvider", control);

            var deps = GetComponent<PlayerDependencies>();
            InputManager input = deps != null ? deps.InputManager : null;
            if (input == null)
            {
                // fallback: search in children/parents
                input = GetComponentInChildren<InputManager>(true);
                if (input == null) input = GetComponentInParent<InputManager>();
            }
            if (input != null) SetPrivateField(camFx, "inputManager", input);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var f = target.GetType().GetField(fieldName, PrivateInstance);
            if (f == null) return;
            f.SetValue(target, value);
        }
    }
}

