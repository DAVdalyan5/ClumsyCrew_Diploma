using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Assets.Scripts.Runtime.Core
{
    /// <summary>
    /// Handles spawning players through VContainer for automatic dependency injection
    /// when clients connect to the network.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform defaultSpawnPoint;
        
        private IObjectResolver container;
        private NetworkManager networkManager;

        [Inject]
        public void Construct(IObjectResolver resolver, NetworkManager netManager)
        {
            container = resolver;
            networkManager = netManager;
        }

        private void Start()
        {
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager not found! PlayerSpawner requires NetworkManager.");
                return;
            }

            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            // Only server spawns players
            if (!networkManager.IsServer) return;

            Debug.Log($"Client {clientId} connected. Spawning player via VContainer...");
            SpawnPlayerForClient(clientId);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!networkManager.IsServer) return;
            
            Debug.Log($"Client {clientId} disconnected.");
        }

        private void SpawnPlayerForClient(ulong clientId)
        {
            // Get spawn position
            Vector3 spawnPosition = defaultSpawnPoint != null 
                ? defaultSpawnPoint.position 
                : Vector3.zero;
            Quaternion spawnRotation = defaultSpawnPoint != null 
                ? defaultSpawnPoint.rotation 
                : Quaternion.identity;

            // VContainer instantiates the player - this automatically injects all [Inject] dependencies!
            GameObject playerInstance = container.Instantiate(
                playerPrefab, 
                spawnPosition, 
                spawnRotation
            );

            // Get the NetworkObject component
            NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
            
            if (networkObject != null)
            {
                // Spawn on the network and assign ownership to the client
                networkObject.SpawnAsPlayerObject(clientId, true);
                Debug.Log($"Player spawned and assigned to client {clientId} with dependency injection complete.");
            }
            else
            {
                Debug.LogError("Player prefab must have a NetworkObject component!");
                Destroy(playerInstance);
            }
        }

        /// <summary>
        /// Manually spawn a player at a specific location (optional, for custom spawn logic)
        /// </summary>
        public void SpawnPlayerAtPosition(ulong clientId, Vector3 position, Quaternion rotation)
        {
            if (!networkManager.IsServer)
            {
                Debug.LogWarning("Only server can spawn players.");
                return;
            }

            GameObject playerInstance = container.Instantiate(playerPrefab, position, rotation);
            NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
            
            if (networkObject != null)
            {
                networkObject.SpawnAsPlayerObject(clientId, true);
            }
        }
    }
}


