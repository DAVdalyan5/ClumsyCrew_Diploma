using System.Collections.Generic;
using Assets.Scripts.Core.Inventory.Models;
using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.Inventory.NetworkedInventory
{
    /// <summary>
    /// Server-authoritative manager for spawning and managing world items.
    /// Singleton NetworkBehaviour that should exist in the scene.
    /// All item spawning goes through this manager to ensure proper network sync.
    /// </summary>
    public class NetworkedItemSpawnManager : NetworkBehaviour
    {
        public static NetworkedItemSpawnManager Instance { get; private set; } //remove all the singleton shit, and make this a shared netowrk object

        [Header("Item Spawning Settings")]
        [SerializeField] private float scatterRadius = 1.5f;
        [SerializeField] private float scatterForce = 3f;
        [SerializeField] private float spawnHeightOffset = 0.5f;

        [Header("Network Item Prefab")]
        [Tooltip("The networked item prefab with NetworkedItem component")]
        [SerializeField] private GameObject networkedItemPrefab;

        // Track all spawned items for cleanup
        private readonly Dictionary<ulong, NetworkedItem> _spawnedItems = new();

        private void Awake()
        {
            //if (Instance != null && Instance != this) //the insatnce is null on non host
            //{
            //    Debug.LogWarning("[NetworkedItemSpawnManager] Duplicate instance detected. Destroying this one.");
            //    Destroy(gameObject);
            //    return;
            //}
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log($"[NetworkedItemSpawnManager] Spawned. IsServer: {IsServer}");

            // Register pre-placed scene items on server
            if (IsServer)
            {//the pre placed items are not visible on non host.
                RegisterSceneItems();
            }
        }

        /// <summary>
        /// Register a pre-placed scene item with the manager.
        /// Called by NetworkedItem during OnNetworkSpawn for items already in scene.
        /// </summary>
        public void RegisterSceneItem(NetworkedItem itemPickup)
        {
            if (!IsServer) return;
            if (itemPickup == null) return;

            var networkObject = itemPickup.GetComponent<NetworkObject>();
            if (networkObject == null) return;

            ulong networkId = networkObject.NetworkObjectId;
            if (!_spawnedItems.ContainsKey(networkId))
            {
                _spawnedItems[networkId] = itemPickup;
                Debug.Log($"[NetworkedItemSpawnManager] Registered scene item: {itemPickup.ItemId} (NetworkId: {networkId})");
            }
        }

        /// <summary>
        /// Find and register all NetworkedItem objects already in the scene.
        /// Called on server when NetworkedItemSpawnManager spawns.
        /// </summary>
        private void RegisterSceneItems()
        {
            var sceneItems = FindObjectsByType<NetworkedItem>(FindObjectsSortMode.None);
            int registeredCount = 0;

            foreach (var item in sceneItems)
            {
                var networkObject = item.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    ulong networkId = networkObject.NetworkObjectId;
                    if (!_spawnedItems.ContainsKey(networkId))
                    {
                        _spawnedItems[networkId] = item;
                        registeredCount++;
                    }
                }
            }

            Debug.Log($"[NetworkedItemSpawnManager] Registered {registeredCount} pre-placed scene items.");
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Spawn an item in the world. Server-only.
        /// </summary>
        /// <param name="itemId">The item ID from ItemRegistry</param>
        /// <param name="position">World position to spawn at</param>
        /// <param name="impactSpeed">Speed for scatter effect (higher = more spread)</param>
        public void SpawnItemInWorld(string itemId, Vector3 position, float impactSpeed = 0f)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkedItemSpawnManager] SpawnItemInWorld called on client. Ignoring.");
                return;
            }

            var itemData = ItemRegistry.GetItemData(itemId);
            if (itemData == null)
            {
                Debug.LogError($"[NetworkedItemSpawnManager] Cannot spawn item: {itemId} not found in registry.");
                return;
            }

            // Calculate spawn position with scatter
            Vector2 randomCircle = Random.insideUnitCircle * scatterRadius;
            Vector3 scatterOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
            Vector3 spawnPosition = position + scatterOffset + Vector3.up * spawnHeightOffset;

            // Determine which prefab to use
            GameObject prefabToSpawn = networkedItemPrefab;
            if (prefabToSpawn == null)
            {
                Debug.LogError("[NetworkedItemSpawnManager] No networked item prefab assigned!");
                return;
            }

            // Instantiate the networked item
            GameObject itemObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

            // Get or add NetworkObject
            var networkObject = itemObject.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogError("[NetworkedItemSpawnManager] Prefab missing NetworkObject component!");
                Destroy(itemObject);
                return;
            }

            // Get the NetworkedItem component
            var itemPickup = itemObject.GetComponent<NetworkedItem>();
            if (itemPickup == null)
            {
                Debug.LogError("[NetworkedItemSpawnManager] Prefab missing NetworkedItem component!");
                Destroy(itemObject);
                return;
            }

            // Spawn on network first
            networkObject.Spawn();

            // Configure the item after spawning (so NetworkVariable sync works)
            itemPickup.InitializeItem(itemId);

            // Track the spawned item
            _spawnedItems[networkObject.NetworkObjectId] = itemPickup;

            // Apply scatter physics
            ApplyScatterPhysics(itemObject, scatterOffset, impactSpeed);

            Debug.Log($"[NetworkedItemSpawnManager] Spawned {itemId} at {spawnPosition}");
        }

        /// <summary>
        /// Spawn item using ItemDataSO reference
        /// </summary>
        public void SpawnItemInWorld(ItemDataSO itemData, Vector3 position, float impactSpeed = 0f)
        {
            if (itemData == null) return;
            SpawnItemInWorld(ItemRegistry.GetItemId(itemData), position, impactSpeed);
        }

        /// <summary>
        /// Despawn an item from the world. Server-only.
        /// Called when an item is picked up.
        /// </summary>
        public void DespawnItem(NetworkObject itemNetworkObject)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkedItemSpawnManager] DespawnItem called on client. Ignoring.");
                return;
            }

            if (itemNetworkObject == null) return;

            _spawnedItems.Remove(itemNetworkObject.NetworkObjectId);
            itemNetworkObject.Despawn();

            Debug.Log($"[NetworkedItemSpawnManager] Despawned item {itemNetworkObject.NetworkObjectId}");
        }

        /// <summary>
        /// Request to pick up an item. Validates and processes on server.
        /// </summary>
        public void RequestPickupItem(ulong itemNetworkId, ulong playerClientId)
        {
            if (!IsServer)
            {
                RequestPickupServerRpc(itemNetworkId, playerClientId);
                return;
            }

            ServerProcessPickup(itemNetworkId, playerClientId);
        }

        [Rpc(SendTo.Server)]
        private void RequestPickupServerRpc(ulong itemNetworkId, ulong playerClientId)
        {
            ServerProcessPickup(itemNetworkId, playerClientId);
        }

        private void ServerProcessPickup(ulong itemNetworkId, ulong playerClientId)
        {
            if (!IsServer) return;

            // Find the item
            if (!_spawnedItems.TryGetValue(itemNetworkId, out var itemPickup))
            {
                Debug.LogWarning($"[NetworkedItemSpawnManager] Item {itemNetworkId} not found for pickup.");
                return;
            }

            if (itemPickup.IsPickedUp)
            {
                Debug.LogWarning($"[NetworkedItemSpawnManager] Item {itemNetworkId} already picked up.");
                return;
            }

            // Find the player's inventory
            var playerInventory = FindPlayerInventory(playerClientId);
            if (playerInventory == null)
            {
                Debug.LogWarning($"[NetworkedItemSpawnManager] Player inventory not found for client {playerClientId}.");
                return;
            }

            // Add item to player inventory
            string itemId = itemPickup.ItemId;
            playerInventory.RequestAddItem(itemId, 1);

            // Mark as picked up and despawn
            itemPickup.MarkAsPickedUp();

            // Despawn after a short delay to allow clients to see the pickup
            var networkObject = itemPickup.GetComponent<NetworkObject>();
            DespawnItem(networkObject);

            Debug.Log($"[NetworkedItemSpawnManager] Player {playerClientId} picked up {itemId}");
        }

        private NetworkedPlayerInventory FindPlayerInventory(ulong clientId)
        {
            // Find all player objects and match by owner
            var players = FindObjectsByType<NetworkedPlayerInventory>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.OwnerClientId == clientId)
                {
                    return player;
                }
            }
            return null;
        }

        private void ApplyScatterPhysics(GameObject itemObject, Vector3 scatterOffset, float impactSpeed)
        {
            var rb = itemObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = itemObject.AddComponent<Rigidbody>();
            }

            // Apply scatter force
            Vector3 scatterDirection = (scatterOffset.normalized + Vector3.up * 0.5f).normalized;
            float forceMagnitude = scatterForce * (1 + impactSpeed * 0.1f);
            rb.AddForce(scatterDirection * forceMagnitude, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
        }

        /// <summary>
        /// Set the scatter configuration
        /// </summary>
        public void SetScatterConfig(float radius, float force)
        {
            scatterRadius = radius;
            scatterForce = force;
        }

        /// <summary>
        /// Set the scatter config from ScatterConfigSO
        /// </summary>
        public void SetScatterConfig(ScatterConfigSO config)
        {
            if (config != null)
            {
                scatterRadius = config.ItemScatterRadius;
                scatterForce = config.ItemScatterForce;
            }
        }
    }
}
