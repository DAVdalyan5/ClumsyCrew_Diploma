using HeistNSeek.Core.Inventory.SessionInventory;
using UnityEngine;
using VContainer;

namespace HeistNSeek.Core.Player
{
    public class PlayerScope
    {
        public IScopedObjectResolver Scope { get; set; }
        public GameObject PlayerInstance { get; set; }
        public SessionInventory SessionInventory { get; set; }
        public ItemDropper ItemDropper { get; set; }
    }
}
