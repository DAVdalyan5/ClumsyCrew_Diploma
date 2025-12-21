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

        public GameplayEntryPoint(EntryPointParameters parameters)
        {
            this.playerSpawnerPrefab = parameters.PlayerSpawnerPrefab;
            this.networkedItemSpawnManager = parameters.NetworkedItemSpawnManager;
        }

        [Inject]
        public void Init(IObjectResolver container)
        {
            this.container = container;
        }

        public void Start()
        {
            NetworkManager.Singleton.OnServerStarted += () => OnServerStarted();
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

        public EntryPointParameters(GameObject playerSpawnerPrefab, GameObject networkedItemSpawnManager)
        {
            PlayerSpawnerPrefab = playerSpawnerPrefab;
            NetworkedItemSpawnManager = networkedItemSpawnManager;
        }
    }
}
