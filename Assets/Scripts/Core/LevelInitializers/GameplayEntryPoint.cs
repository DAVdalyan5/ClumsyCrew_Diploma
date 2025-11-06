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
        public void Start()
        {
            Debug.Log("[GameplayEntryPoint] Gameplay scene initialized.");
            // Add Gameplay-specific initialization logic here
        }
    }
}
