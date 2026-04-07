using HeistNSeek.Core.Inventory.NetworkedInventory;
using HeistNSeek.Core.Inventory.SessionInventory;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample.DistributedAuthority;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using WebSocketSharp.Server;

namespace HeistNSeek.Core
{
    /// <summary>
    /// EntryPoint for Gameplay/Main scene initialization.
    /// Runs after all dependencies are resolved in GameplayLifetimeScope.
    /// </summary>
    public class GameplayEntryPoint : IStartable
    {
        private IObjectResolver container;
        private GameObject playerSpawnerPrefab;
        private GameObject networkedItemSpawnManager;
        private GameObject cowsinsSessionServicesPrefab;

        public GameplayEntryPoint(EntryPointParameters parameters)
        {
            this.playerSpawnerPrefab = parameters.PlayerSpawnerPrefab;
            this.networkedItemSpawnManager = parameters.NetworkedItemSpawnManager;
            this.cowsinsSessionServicesPrefab = parameters.CowsinsSessionServicesPrefab;
        }

        [Inject]
        public void Init(IObjectResolver container)
        {
            this.container = container;
        }

        public void Start()
        {
            EnsureCowsinsSessionServices();
            NetworkManager.Singleton.OnServerStarted += () => OnServerStarted();
        }

        private static GameObject _cowsinsSessionServicesInstance;

        private void EnsureCowsinsSessionServices()
        {
            if (_cowsinsSessionServicesInstance != null)
                return;
            if (cowsinsSessionServicesPrefab == null)
            {
                Debug.LogError("[GameplayEntryPoint] Cowsins session services prefab is not assigned on GameplayLifetimeScope.");
                return;
            }

            _cowsinsSessionServicesInstance = container.Instantiate(cowsinsSessionServicesPrefab, Vector3.zero, Quaternion.identity, null);
        }

        private void OnServerStarted()
        {
            CreateSpawner();
            CreateNetworkedItemSpawnManager();
        }

        private void CreateNetworkedItemSpawnManager()
        {
            var itemSpawnManager = container.Instantiate(networkedItemSpawnManager);

            var networkObjectSpawner = itemSpawnManager.GetComponent<NetworkObject>();
            networkObjectSpawner.Spawn();
        }

        private void CreateSpawner()
        {
            if (!NetworkManager.Singleton.IsServer) return;

            var spawner = container.Instantiate(playerSpawnerPrefab);

            var networkObjectSpawner = spawner.GetComponent<NetworkObject>();
            networkObjectSpawner.Spawn();
        }
    }

    public class EntryPointParameters
    {
        public GameObject PlayerSpawnerPrefab { get; }
        public GameObject NetworkedItemSpawnManager { get; }
        public GameObject CowsinsSessionServicesPrefab { get; }

        public EntryPointParameters(GameObject playerSpawnerPrefab, GameObject networkedItemSpawnManager, GameObject cowsinsSessionServicesPrefab)
        {
            PlayerSpawnerPrefab = playerSpawnerPrefab;
            NetworkedItemSpawnManager = networkedItemSpawnManager;
            CowsinsSessionServicesPrefab = cowsinsSessionServicesPrefab;
        }
    }
}
