using HeistNSeek.Core.Inventory.SessionInventory;
using UnityEngine;
using VContainer.Unity;

namespace HeistNSeek.Core
{
    /// <summary>
    /// EntryPoint for Gameplay/Main scene initialization.
    /// Runs after all dependencies are resolved in GameplayLifetimeScope.
    /// </summary>
    public class GameplayEntryPoint : IStartable
    {
        private readonly ItemDropper _itemDropper;

        public GameplayEntryPoint(ItemDropper itemDropper)
        {
            _itemDropper = itemDropper;
        }

        public void Start()
        {
            Debug.Log("[GameplayEntryPoint] Gameplay scene initialized.");
            Debug.Log("[GameplayEntryPoint] ItemDropper service initialized and ready.");
            // Add Gameplay-specific initialization logic here
        }
    }
}
