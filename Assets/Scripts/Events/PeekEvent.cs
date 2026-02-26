using HeistNSeek.Core.Inventory.NetworkedInventory;

namespace HeistNSeek.Events
{
    /// <summary>
    /// Published when the local player peeks at another player's inventory.
    /// </summary>
    public class PeekEvent
    {
        public NetworkedPlayerInventory TargetInventory { get; }

        public PeekEvent(NetworkedPlayerInventory targetInventory)
        {
            TargetInventory = targetInventory;
        }
    }
}
