using Unity.Netcode;

namespace Assets.Scripts.Runtime.Helpers
{
    /// <summary>
    /// Extension methods for NetworkBehaviour to simplify ownership checks.
    /// </summary>
    public static class NetworkBehaviourExtensions
    {
        /// <summary>
        /// Checks if this NetworkBehaviour is owned by the local client.
        /// Returns true if the NetworkBehaviour is null (standalone/non-networked mode).
        /// </summary>
        public static bool IsOwnerOrStandalone(this NetworkBehaviour networkBehaviour)
        {
            return networkBehaviour == null || networkBehaviour.IsOwner;
        }
    }
}

