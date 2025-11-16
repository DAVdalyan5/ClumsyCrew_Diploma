using Assets.Scripts.Core.Inventory.Models;
using Assets.Scripts.Core.Player;
using Assets.Scripts.Core.Player.Character;
using Easy.MessageHub;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeistNSeek.Core.Inventory.SessionInventory
{
    /// <summary>
    /// Service that handles dropping items from inventory when player loses balance.
    /// Listens to BalanceLostEvent and spawns items in the scene with scatter effect.
    /// </summary>
    public class ItemDropper : IDisposable
    {
        private readonly SessionInventory inventory;
        private readonly IMessageHub messageHub;
        private readonly Transform playerTransform;
        private readonly ScatterConfigSO scatterConfigs;

        private Guid _balanceLostSubscription;

        // Store reference to allow manual injection for spawned items
        private static SessionInventory _staticInventoryRef;

        /// <summary>
        /// Gets the current session inventory instance.
        /// Used by ItemPickup components that are not in the DI hierarchy.
        /// </summary>
        public static SessionInventory GetInventory() => _staticInventoryRef;

        public ItemDropper(SessionInventory inventory, IMessageHub messageHub, PlayerController playerController, ScatterConfigSO scatterConfig)
        {
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            scatterConfigs = scatterConfig ?? throw new ArgumentNullException(nameof(messageHub));
            Debug.Log($"INJECTION HAPPENED:FORCE + {scatterConfigs.ItemScatterForce}: RADIUS + {scatterConfigs.ItemScatterRadius}");

            if (playerController == null)
                throw new ArgumentNullException(nameof(playerController));

            playerTransform = playerController.transform;
            _staticInventoryRef = this.inventory; // Store for ItemPickup access

            // Subscribe to balance lost events
            _balanceLostSubscription = this.messageHub.Subscribe<BalanceLostEvent>(OnBalanceLost);
        }

        private void OnBalanceLost(BalanceLostEvent evt)
        {
            float impactSpeed = evt.ImpactSpeed;
             
            // Drop items from inventory
            Vector3 dropPosition = playerTransform.position;
            var droppedItems = inventory.DropItems(dropPosition, impactSpeed);

            if (droppedItems.Count > 0)
            {
                SpawnDroppedItems(droppedItems, dropPosition, impactSpeed);
                Debug.Log($"[ItemDropper] Dropped {droppedItems.Count} items due to impact speed {impactSpeed:F1}");
            }
        }

        /// <summary>
        /// Spawns dropped items in the scene with scatter effect.
        /// </summary>
        private void SpawnDroppedItems(List<ItemDataSO> droppedItems, Vector3 dropPosition, float impactSpeed)
        {
            foreach (var itemData in droppedItems)
            {
                if (itemData.prefab == null)
                {
                    Debug.LogWarning($"[ItemDropper] No prefab assigned for {itemData.itemName}, cannot spawn.");
                    continue;
                }

                // Calculate scatter position
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * scatterConfigs.ItemScatterRadius;
                Vector3 scatterOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
                Vector3 spawnPosition = dropPosition + scatterOffset + Vector3.up * 0.5f; // Spawn slightly above ground

                // Instantiate item
                GameObject itemObject = GameObject.Instantiate(itemData.prefab, spawnPosition, Quaternion.identity);

                // Add ItemPickup component if not present
                var pickup = itemObject.GetComponent<ItemPickup>();
                if (pickup == null)
                {
                    pickup = itemObject.AddComponent<ItemPickup>();
                    Debug.LogWarning($"[ItemDropper] ItemPickup not found on {itemData.itemName} prefab, adding dynamically.");
                }

                // Manually inject inventory into spawned item
                pickup.Init(inventory);

                // Apply scatter physics
                var rb = itemObject.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = itemObject.AddComponent<Rigidbody>();
                }

                // Apply random scatter force
                Vector3 scatterDirection = (scatterOffset.normalized + Vector3.up * 0.5f).normalized;
                float forceMagnitude = scatterConfigs.ItemScatterForce; //* (1 + impactSpeed);
                rb.AddForce(scatterDirection * forceMagnitude, ForceMode.Impulse);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 2f, ForceMode.Impulse);

                Debug.Log($"[ItemDropper] Spawned {itemData.itemName} at {spawnPosition}");
            }
        }

        public void Dispose()
        {
            this.messageHub.Unsubscribe(_balanceLostSubscription);
            Debug.Log("[ItemDropper] Disposed and unsubscribed from BalanceLostEvent.");
        }
    }
}
