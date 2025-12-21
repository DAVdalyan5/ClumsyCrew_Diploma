using System.Collections.Generic;
using Assets.Scripts.Core.Inventory.Models;
using UnityEngine;

namespace HeistNSeek.Core.Inventory.NetworkedInventory
{
    /// <summary>
    /// Static registry that maps item IDs to ItemDataSO.
    /// Used for network serialization - we send IDs over network and lookup the actual data locally.
    /// Must be initialized with all available items at game start.
    /// </summary>
    public static class ItemRegistry
    {
        private static Dictionary<string, ItemDataSO> _items = new();
        private static bool _initialized = false;

        /// <summary>
        /// Initialize the registry with all available items.
        /// Call this once at game startup (e.g., in BootstrapEntryPoint).
        /// </summary>
        public static void Initialize(IEnumerable<ItemDataSO> allItems)
        {
            _items.Clear();

            foreach (var item in allItems)
            {
                if (item == null) continue;

                string id = GetItemId(item);
                if (_items.ContainsKey(id))
                {
                    Debug.LogWarning($"[ItemRegistry] Duplicate item ID: {id}. Skipping duplicate.");
                    continue;
                }

                _items[id] = item;
                Debug.Log($"[ItemRegistry] Registered item: {id}");
            }

            _initialized = true;
            Debug.Log($"[ItemRegistry] Initialized with {_items.Count} items.");
        }

        /// <summary>
        /// Initialize from a folder of ScriptableObjects (Resources path)
        /// </summary>
        public static void InitializeFromResources(string resourcePath = "Items")
        {
            var items = Resources.LoadAll<ItemDataSO>(resourcePath);
            Initialize(items);
        }

        /// <summary>
        /// Get the unique ID for an item. Uses the ScriptableObject name.
        /// </summary>
        public static string GetItemId(ItemDataSO item)
        {
            if (item == null) return string.Empty;
            return item.name; // Use SO asset name as unique ID
        }

        /// <summary>
        /// Get ItemDataSO by ID.
        /// </summary>
        public static ItemDataSO GetItemData(string itemId)
        {
            if (!_initialized)
            {
                Debug.LogWarning("[ItemRegistry] Not initialized! Call Initialize() first.");
                return null;
            }

            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            if (_items.TryGetValue(itemId, out var itemData))
            {
                return itemData;
            }

            Debug.LogWarning($"[ItemRegistry] Item not found: {itemId}");
            return null;
        }

        /// <summary>
        /// Check if an item ID exists in the registry.
        /// </summary>
        public static bool HasItem(string itemId)
        {
            return _items.ContainsKey(itemId);
        }

        /// <summary>
        /// Get all registered items.
        /// </summary>
        public static IEnumerable<ItemDataSO> GetAllItems()
        {
            return _items.Values;
        }

        /// <summary>
        /// Clear the registry (for testing or reset purposes)
        /// </summary>
        public static void Clear()
        {
            _items.Clear();
            _initialized = false;
        }

        /// <summary>
        /// Register a single item (useful for runtime-added items)
        /// </summary>
        public static void RegisterItem(ItemDataSO item)
        {
            if (item == null) return;

            string id = GetItemId(item);
            if (!_items.ContainsKey(id))
            {
                _items[id] = item;
                Debug.Log($"[ItemRegistry] Registered item: {id}");
            }
        }
    }
}
