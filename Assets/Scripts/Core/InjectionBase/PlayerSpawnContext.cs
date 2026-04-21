using UnityEngine;

namespace HeistNSeek.Core
{
    /// <summary>
    /// Scene-configured player spawn marker (world position and rotation). Registered in GameplayLifetimeScope.Configure.
    /// </summary>
    public sealed class PlayerSpawnContext
    {
        public Transform SpawnTransform { get; }

        public PlayerSpawnContext(Transform spawnTransform)
        {
            SpawnTransform = spawnTransform;
        }
    }
}
