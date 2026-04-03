using UnityEngine;

namespace Assets.Scripts.Events
{
    /// <summary>
    /// Published on the local client once the local player has spawned
    /// and its camera has been wired up. Subscribe to this to obtain
    /// runtime references to the local player camera and root transform.
    /// </summary>
    public class LocalPlayerSpawnedEvent
    {
        public Camera PlayerCamera { get; }
        public Transform PlayerTransform { get; }

        public LocalPlayerSpawnedEvent(Camera playerCamera, Transform playerTransform)
        {
            PlayerCamera = playerCamera;
            PlayerTransform = playerTransform;
        }
    }
}
