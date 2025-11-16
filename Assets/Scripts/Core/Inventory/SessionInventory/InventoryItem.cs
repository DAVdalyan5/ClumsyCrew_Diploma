using Assets.Scripts.Core.Inventory.Models;

namespace HeistNSeek.Core.Inventory.SessionInventory
{
    /// <summary>
    /// Represents an item in the inventory with its stack count.
    /// Used by SessionInventory to track items and their quantities.
    /// </summary>
    public class InventoryItem
    {
        public ItemDataSO ItemData { get; private set; }
        public int CurrentStackCount { get; private set; }

        public InventoryItem(ItemDataSO itemData, int stackCount = 1)
        {
            ItemData = itemData;
            CurrentStackCount = stackCount;
        }

        /// <summary>
        /// Adds to the stack count. Returns true if successful, false if exceeds max stack size.
        /// </summary>
        public bool AddToStack(int amount)
        {
            if (!ItemData.isStackable)
                return false;

            int newCount = CurrentStackCount + amount;
            if (newCount > ItemData.maxStackSize)
                return false;

            CurrentStackCount = newCount;
            return true;
        }

        /// <summary>
        /// Removes from the stack count. Returns the actual amount removed.
        /// </summary>
        public int RemoveFromStack(int amount)
        {
            int actualRemoved = UnityEngine.Mathf.Min(amount, CurrentStackCount);
            CurrentStackCount -= actualRemoved;
            return actualRemoved;
        }

        /// <summary>
        /// Checks if this item can stack with another item data.
        /// </summary>
        public bool CanStackWith(ItemDataSO otherItemData)
        {
            return ItemData.isStackable &&
                   ItemData == otherItemData &&
                   CurrentStackCount < ItemData.maxStackSize;
        }
    }
}
