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
            var networkObject = playerInstance.GetComponent<NetworkObject>();

            // Client-side injection will be handled by ClientSidePlayerConfigurator.OnNetworkSpawn
            networkObject.SpawnAsPlayerObject(clientId);
        }

        public void SpawnPlayer(ulong id, Vector3 position = default)
        {
            SpawnPlayerForClient(id);
        }
    }
}


