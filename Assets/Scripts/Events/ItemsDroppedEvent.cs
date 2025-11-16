using System.Collections.Generic;
using Assets.Scripts.Core.Inventory.Models;
using UnityEngine;

namespace HeistNSeek.Events
{
    /// <summary>
    /// Published when items are dropped from the inventory (e.g., when balance is lost).
    /// </summary>
    public class ItemsDroppedEvent
    {
        public List<ItemDataSO> DroppedItems { get; }
        public Vector3 DropPosition { get; }
        public float ImpactSpeed { get; }

        public ItemsDroppedEvent(List<ItemDataSO> droppedItems, Vector3 dropPosition, float impactSpeed)
        {
            DroppedItems = droppedItems;
            DropPosition = dropPosition;
            ImpactSpeed = impactSpeed;
        }
    }
}
