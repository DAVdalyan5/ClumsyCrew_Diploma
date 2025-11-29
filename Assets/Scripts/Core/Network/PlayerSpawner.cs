using Assets.Scripts.Core.Player;
using Cinemachine;
using HeistNSeek.Core;
using HeistNSeek.Core.Player;
using Unity.Netcode;
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
        [SerializeField] private CinemachineVirtualCamera playerFollowCamera;
        [SerializeField] private Camera mainCamera;

        [SerializeField] private GameObject playerPrefab;

        private NetworkManager networkManager;
        private IObjectResolver container;

        [Inject]
        public void Init(IObjectResolver container)
        {
            this.container = container;
        }

        private void Awake()
        {
            networkManager = NetworkManager.Singleton;
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
            if (!IsServer) return;

            Vector3 spawnPosition = Vector3.zero;

            var playerInstance = container.Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            var networkObject = playerInstance.GetComponent<NetworkObject>();

            ClientSideSetupRpc();

            networkObject.SpawnAsPlayerObject(clientId);
        }

        [Rpc(SendTo.Owner)]
        private void ClientSideSetupRpc()
        {
            var playerInstance = FindFirstObjectByType<PlayerController>().gameObject;

            var injector = playerInstance.GetComponent<ClientSidePlayerConfigurator>();
            if (injector != null)
            {
                injector.SetContainer(container);
            }
        }

        public void SpawnPlayer(ulong id, Vector3 position = default)
        {
            SpawnPlayerForClient(id);
        }
    }
}


