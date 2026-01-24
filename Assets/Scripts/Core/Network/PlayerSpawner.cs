using Assets.Scripts.Core.Player;
using Cinemachine;
using HeistNSeek.Core;
using HeistNSeek.Core.Player;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Assets.Scripts.Runtime.Core
{
    /// <summary>
    /// Handles spawning players with per-player dependency injection scopes
    /// when clients connect to the network.
    /// </summary>
    public class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private GameObject playerPrefab;

        private GameObject playerInstance;
        private IObjectResolver container;

        [Inject]
        public void Init(IObjectResolver container)
        {
            this.container = container;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            SpawnPlayerForClient(clientId);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log("Disconnect Sequence starts here");
        }

        private void SpawnPlayerForClient(ulong clientId)
        {
            if (!IsServer) return;

            Vector3 spawnPosition = Vector3.zero;

            // IMPORTANT (Netcode):
            // VContainer's 3-arg Instantiate(prefab, pos, rot) may temporarily parent the instance under the LifetimeScope
            // and then SetParent(null). Netcode throws SpawnStateException if a NetworkObject is reparented before Spawn().
            // Use the overload with explicit parent = null to avoid any pre-spawn parenting.
            playerInstance = container.Instantiate(playerPrefab, spawnPosition, Quaternion.identity, null);

            StripNestedNetworkObjects(playerInstance);

            var networkObject = playerInstance.GetComponent<NetworkObject>();

            // Client-side injection will be handled by ClientSidePlayerConfigurator.OnNetworkSpawn
            networkObject.SpawnAsPlayerObject(clientId);
        }

        private static void StripNestedNetworkObjects(GameObject instanceRoot)
        {
            if (instanceRoot == null) return;

            var all = instanceRoot.GetComponentsInChildren<NetworkObject>(true);
            if (all == null || all.Length <= 1) return;

            // Keep the root NetworkObject only.
            var rootNo = instanceRoot.GetComponent<NetworkObject>();
            int removedNetworkObjects = 0;
            int removedNetworkBehaviours = 0;
            string removedNames = "";
            string removedBehaviourTypes = "";

            foreach (var no in all)
            {
                if (no == null) continue;
                if (rootNo != null && no == rootNo) continue;

                var go = no.gameObject;

                // Child NetworkObjects on spawned prefabs are not supported by Netcode.
                // Unity won't let us remove a NetworkObject while NetworkBehaviour components still depend on it.
                // Also, Destroy() is end-of-frame, too late for SpawnAsPlayerObject() validation.
                var nbs = go.GetComponents<NetworkBehaviour>();
                for (int i = 0; i < nbs.Length; i++)
                {
                    var nb = nbs[i];
                    if (nb == null) continue;
                    removedNetworkBehaviours++;
                    if (removedNetworkBehaviours <= 8)
                        removedBehaviourTypes += (removedBehaviourTypes.Length == 0 ? "" : ",") + nb.GetType().Name;
                    Object.DestroyImmediate(nb);
                }

                removedNetworkObjects++;
                if (removedNetworkObjects <= 5) removedNames += (removedNames.Length == 0 ? "" : ",") + go.name;
                Object.DestroyImmediate(no);
            }
        }

        public void SpawnPlayer(ulong id, Vector3 position = default)
        {
            SpawnPlayerForClient(id);
        }
    }
}


