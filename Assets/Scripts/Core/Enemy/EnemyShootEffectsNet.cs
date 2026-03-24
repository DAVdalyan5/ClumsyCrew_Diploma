using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.Enemy
{
    /// <summary>
    /// Server fires hitscan on the host only; this replays Cowsins-style muzzle/audio on other connected clients.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class EnemyShootEffectsNet : NetworkBehaviour
    {
        private EnemyWeaponController _weapon;

        private void Awake()
        {
            _weapon = GetComponent<EnemyWeaponController>();
        }

        public void NotifyRemoteClientsShotVisuals()
        {
            if (!IsServer)
                return;

            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening)
                return;

            if (nm.ConnectedClients.Count <= 1)
                return;

            SpawnVisualsOnClientsClientRpc();
        }

        [ClientRpc]
        private void SpawnVisualsOnClientsClientRpc()
        {
            _weapon?.SpawnCowinsShotVisualsLocal();
        }
    }
}
