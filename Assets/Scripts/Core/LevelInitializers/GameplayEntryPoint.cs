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

        public GameplayEntryPoint(GameObject playerSpawnerPrefab)
        {
            this.playerSpawnerPrefab = playerSpawnerPrefab;
        }

        [Inject]
        public void Init(IObjectResolver container)
        {
            this.container = container;
        }

        public void Start()
        {
            NetworkManager.Singleton.OnServerStarted += () => CreateSpawner();
        }

        private void CreateSpawner()
        {
            if (!NetworkManager.Singleton.IsServer) return;

            var spawner = container.Instantiate(playerSpawnerPrefab);

            var networkObjectSpawner = spawner.GetComponent<NetworkObject>();
            networkObjectSpawner.Spawn();
        }
    }
}
