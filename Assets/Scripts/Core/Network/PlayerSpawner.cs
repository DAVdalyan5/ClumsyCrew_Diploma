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
            playerProvider = provider;
            networkManager = netManager;
        }

        private void Start()
        {
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
            SpawnPlayerForClient(clientId);
        }

        private void OnClientDisconnected(ulong clientId)
        {
        }

        private void SpawnPlayerForClient(ulong clientId)
        {
            Vector3 spawnPosition = defaultSpawnPoint != null
                ? defaultSpawnPoint.position
                : Vector3.zero;

            GameObject playerInstance = playerProvider.CreatePlayer(
                clientId,
                playerPrefab,
                spawnPosition,
                Quaternion.identity
            );

            var followTransform = playerInstance.GetComponentInChildren<CameraRootMarker>()?.transform;
            var pusher = playerInstance.GetComponentInChildren<CharacterPusher>();
            if (playerFollowCamera != null)
            {
                playerFollowCamera.Follow = followTransform;
                pusher.CameraTransform = mainCamera.transform;
            }
        }

        public void SpawnPlayer(ulong id, Vector3 position = default)
        {
            SpawnPlayerForClient(id);
        }
    }
}


