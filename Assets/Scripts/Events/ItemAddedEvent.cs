using Assets.Scripts.Core.Inventory.Models;

namespace HeistNSeek.Events
{
    /// <summary>
    /// Published when an item is added to the session inventory.
    /// </summary>
    public class ItemAddedEvent
    {
        public ItemDataSO ItemData { get; }
        public int Amount { get; }
        public int TotalCount { get; }

        public ItemAddedEvent(ItemDataSO itemData, int amount, int totalCount)
        {
            ItemData = itemData;
            Amount = amount;
            TotalCount = totalCount;
        }
    }
}
