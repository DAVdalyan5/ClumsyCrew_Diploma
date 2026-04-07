using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Scene fallback <see cref="AudioListener"/>: enabled while no local player listener is active
    /// (prevents Unity warnings before the networked player spawns). Disabled in <see cref="LateUpdate"/>
    /// once the local player's camera rig listener is active so only one listener runs per client.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioListener))]
    public class LocalPlayerAudioListenerHandoff : MonoBehaviour
    {
        private AudioListener _fallback;

        private void Awake()
        {
            _fallback = GetComponent<AudioListener>();
        }

        private void LateUpdate()
        {
            if (_fallback == null)
                return;

            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer && !nm.IsClient)
            {
                _fallback.enabled = false;
                return;
            }

            _fallback.enabled = !LocalPlayerHasActiveAudioListener();
        }

        private static bool LocalPlayerHasActiveAudioListener()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null)
                return false;

            var player = nm.LocalClient != null ? nm.LocalClient.PlayerObject : null;
            if (player == null)
                return false;

            var listeners = player.GetComponentsInChildren<AudioListener>(true);
            for (var i = 0; i < listeners.Length; i++)
            {
                var l = listeners[i];
                if (l != null && l.enabled && l.gameObject.activeInHierarchy)
                    return true;
            }

            return false;
        }
    }
}
