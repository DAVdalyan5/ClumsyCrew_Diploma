using Assets.Scripts.Core.Inventory.Models;

namespace HeistNSeek.Events
{
    /// <summary>
    /// Published when an item is removed from the session inventory.
    /// </summary>
    public class ItemRemovedEvent
    {
        public ItemDataSO ItemData { get; }
        public int Amount { get; }
        public int RemainingCount { get; }

        public ItemRemovedEvent(ItemDataSO itemData, int amount, int remainingCount)
        {
            ItemData = itemData;
            Amount = amount;
            RemainingCount = remainingCount;
        }
    }
}
