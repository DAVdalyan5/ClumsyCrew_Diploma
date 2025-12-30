namespace HeistNSeek.Core.Inventory.NetworkedInventory
{
    public interface ICollectable
    {
        void ExecutePickup();

        /// <summary>
        /// Force pickup by a specific player, bypassing trigger-based proximity checks.
        /// Used for sphere/raycast-based pickup systems.
        /// </summary>
        /// <param name="playerClientId">The client ID of the player picking up the item</param>
        void ForcePickup(ulong playerClientId);
    }
}