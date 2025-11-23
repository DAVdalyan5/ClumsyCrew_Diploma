using Assets.Scripts.Core.Player;
using Cinemachine;
using HeistNSeek.Core;
using HeistNSeek.Core.Player;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Assets.Scripts.Runtime.Core
{
    /// <summary>
    /// Handles spawning players with per-player dependency injection scopes
    /// when clients connect to the network.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform defaultSpawnPoint;

        [SerializeField] private CinemachineVirtualCamera playerFollowCamera;
        [SerializeField] private Camera mainCamera;

        private PlayerProvider playerProvider;
        private NetworkManager networkManager;

        [Inject]
        public void Construct(PlayerProvider provider, NetworkManager netManager)
        {
            Debug.Log("PlayerSpawner Constructed with PlayerProvider and NetworkManager.");
            playerProvider = provider;
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
            if (!networkManager.IsServer)
            {
                Debug.Log("[PlayerSpawner] Not server. Ignoring spawn.");
                return;
            }

            Debug.Log($"Client {clientId} connected. Spawning player via VContainer...");
            SpawnPlayerForClient(clientId);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!networkManager.IsServer) return;

            Debug.Log($"Client {clientId} disconnected. Cleaning up player...");
            playerProvider.RemovePlayer(clientId);
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

            // PlayerProvider creates player with per-player DI scope
            // This ensures each player gets isolated SessionInventory and ItemDropper instances
            GameObject playerInstance = playerProvider.CreatePlayer(       //provider not being injected
                clientId,
                playerPrefab,
                spawnPosition,
                spawnRotation
            );

            if (playerInstance == null)
            {
                Debug.LogError($"Failed to create player for client {clientId}");
                return;
            }

            var followTransform = playerInstance.GetComponentInChildren<CameraRootMarker>()?.transform;
            var pusher = playerInstance.GetComponentInChildren<CharacterPusher>();
            if (playerFollowCamera != null)
            {
                playerFollowCamera.Follow = followTransform;
                pusher.CameraTransform = mainCamera.transform;
                //playerFollowCamera.LookAt = playerTransform;
            }

            // Get the NetworkObject component
            NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

            if (networkObject != null)
            {
                // Spawn on the network and assign ownership to the client
                networkObject.SpawnAsPlayerObject(clientId, true);
                Debug.Log($"Player spawned and assigned to client {clientId} with per-player dependency injection complete.");
            }
            else
            {
                Debug.LogError("Player prefab must have a NetworkObject component!");
                playerProvider.RemovePlayer(clientId);
            }
        }

        /// <summary>
        /// Manually spawn a player at a specific location (optional, for custom spawn logic)
        /// </summary>
        private void SpawnPlayerAtPosition(ulong clientId, Vector3 position, Quaternion rotation)
        {
            if (!networkManager.IsServer)
            {
                Debug.LogWarning("Only server can spawn players.");
                return;
            }

            GameObject playerInstance = playerProvider.CreatePlayer(clientId, playerPrefab, position, rotation);

            if (playerInstance == null)
            {
                Debug.LogError($"Failed to create player for client {clientId}");
                return;
            }

            NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

            if (networkObject != null)
            {
                networkObject.SpawnAsPlayerObject(clientId, true);
            }
            else
            {
                Debug.LogError("Player prefab must have a NetworkObject component!");
                playerProvider.RemovePlayer(clientId);
            }
        }

        public void SpawnPlayer(ulong id, Vector3 position = default)
        {
            if (position == default)
            {
                SpawnPlayerForClient(id);
                return;
            }
            SpawnPlayerAtPosition(id, position, Quaternion.identity);
        }
    }
}


