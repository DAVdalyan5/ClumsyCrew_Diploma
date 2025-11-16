using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Core.Inventory.Models;
using Easy.MessageHub;
using HeistNSeek.Events;
using UnityEngine;

namespace HeistNSeek.Core.Inventory.SessionInventory
{
    /// <summary>
    /// Manages the player's session inventory (backpack).
    /// Handles item collection, stacking, removal, and dropping when balance is lost.
    /// </summary>
    public class SessionInventory
    {
        private readonly IMessageHub _messageHub;
        private readonly List<InventoryItem> _items;

        public IReadOnlyList<InventoryItem> Items => _items.AsReadOnly();

        public int TotalItemCount => _items.Sum(item => item.CurrentStackCount);
        public float TotalWeight => _items.Sum(item => item.ItemData.weight * item.CurrentStackCount);

        public SessionInventory(IMessageHub messageHub)
        {
            _messageHub = messageHub ?? throw new System.ArgumentNullException(nameof(messageHub));
            _items = new List<InventoryItem>();
        }

        /// <summary>
        /// Adds an item to the inventory. Stacks if possible, creates new entry otherwise.
        /// </summary>
        /// <returns>True if item was added successfully.</returns>
        public bool AddItem(ItemDataSO itemData, int amount = 1)
        {
            if (itemData == null || amount <= 0)
                return false;

            // Filter out weapons - this inventory only holds non-weapon items
            if (itemData is WeaponDataSO)
            {
                Debug.LogWarning($"[SessionInventory] Cannot add weapon '{itemData.itemName}' to item inventory.");
                return false;
            }

            int remainingAmount = amount;

            // Try to stack with existing items first
            if (itemData.isStackable)
            {
                foreach (var inventoryItem in _items)
                {
                    if (inventoryItem.CanStackWith(itemData))
                    {
                        int canAdd = Mathf.Min(remainingAmount, itemData.maxStackSize - inventoryItem.CurrentStackCount);
                        inventoryItem.AddToStack(canAdd);
                        remainingAmount -= canAdd;

                        if (remainingAmount <= 0)
                            break;
                    }
                }
            }

            // Create new inventory entries for remaining items
            while (remainingAmount > 0)
            {
                int stackSize = itemData.isStackable
                    ? Mathf.Min(remainingAmount, itemData.maxStackSize)
                    : 1;

                var newItem = new InventoryItem(itemData, stackSize);
                _items.Add(newItem);
                remainingAmount -= stackSize;
            }

            int totalCount = GetItemCount(itemData);
            _messageHub.Publish(new ItemAddedEvent(itemData, amount, totalCount));

            Debug.Log($"[SessionInventory] Added {amount}x '{itemData.itemName}'. Total: {totalCount}");
            return true;
        }

        /// <summary>
        /// Removes an item from the inventory.
        /// </summary>
        /// <returns>True if item was removed successfully.</returns>
        public bool RemoveItem(ItemDataSO itemData, int amount = 1)
        {
            if (itemData == null || amount <= 0)
                return false;

            int remainingToRemove = amount;

            for (int i = _items.Count - 1; i >= 0 && remainingToRemove > 0; i--)
            {
                var inventoryItem = _items[i];
                if (inventoryItem.ItemData != itemData)
                    continue;

                int removed = inventoryItem.RemoveFromStack(remainingToRemove);
                remainingToRemove -= removed;

                if (inventoryItem.CurrentStackCount <= 0)
                    _items.RemoveAt(i);
            }

            int actualRemoved = amount - remainingToRemove;
            if (actualRemoved > 0)
            {
                int remainingCount = GetItemCount(itemData);
                _messageHub.Publish(new ItemRemovedEvent(itemData, actualRemoved, remainingCount));

                Debug.Log($"[SessionInventory] Removed {actualRemoved}x '{itemData.itemName}'. Remaining: {remainingCount}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the total count of a specific item in the inventory.
        /// </summary>
        public int GetItemCount(ItemDataSO itemData)
        {
            return _items
                .Where(item => item.ItemData == itemData)
                .Sum(item => item.CurrentStackCount);
        }

        /// <summary>
        /// Checks if the inventory contains at least the specified amount of an item.
        /// </summary>
        public bool HasItem(ItemDataSO itemData, int amount = 1)
        {
            return GetItemCount(itemData) >= amount;
        }

        /// <summary>
        /// Clears all items from the inventory.
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            Debug.Log("[SessionInventory] Inventory cleared.");
        }

        /// <summary>
        /// Drops a percentage of items from the inventory.
        /// Used when player loses balance - the harder the fall, the more items dropped.
        /// </summary>
        /// <param name="dropPercentage">Percentage of items to drop (0-1)</param>
        /// <param name="dropPosition">World position where items should be dropped</param>
        /// <param name="impactSpeed">Impact speed of the fall (used to determine scatter)</param>
        /// <returns>List of ItemDataSO that were dropped</returns>
        public List<ItemDataSO> DropItems(Vector3 dropPosition, float impactSpeed)
        {
            int itemsToDrop = Mathf.CeilToInt(TotalItemCount);

            if (itemsToDrop <= 0)
                return new List<ItemDataSO>();

            var droppedItems = new List<ItemDataSO>();
            int droppedCount = 0;

            // Drop items randomly from the inventory
            while (droppedCount < itemsToDrop && _items.Count > 0)
            {
                // Pick a random item
                int randomIndex = Random.Range(0, _items.Count);
                var inventoryItem = _items[randomIndex];

                // Remove one from stack
                inventoryItem.RemoveFromStack(1);
                droppedItems.Add(inventoryItem.ItemData);
                droppedCount++;

                // Remove from inventory if stack is empty
                if (inventoryItem.CurrentStackCount <= 0)
                    _items.RemoveAt(randomIndex);
            }

            if (droppedItems.Count > 0)
            {
                _messageHub.Publish(new ItemsDroppedEvent(droppedItems, dropPosition, impactSpeed));
                Debug.Log($"[SessionInventory] Dropped {droppedItems.Count} items at {dropPosition} (impact: {impactSpeed})");
            }

            return droppedItems;
        }
    }
}
