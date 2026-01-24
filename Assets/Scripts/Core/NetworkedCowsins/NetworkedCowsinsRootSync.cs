using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Keeps the networked root transform in sync with the actually-moving Cowsins "Player" transform.
    ///
    /// Rationale:
    /// - We keep a single spawned NetworkObject on the prefab root (required by your spawner).
    /// - Cowsins moves a child ("Player") with Rigidbody/PlayerMovement.
    /// - We avoid nested NetworkObjects (Netcode throws SpawnStateException on reparent-before-spawn).
    ///
    /// Owner: root follows Player.
    /// Non-owner: Player follows root.
    /// </summary>
    [DisallowMultipleComponent]
    public class NetworkedCowsinsRootSync : NetworkBehaviour
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private bool syncRotation = true;

        private void Awake()
        {
            if (playerTransform == null)
            {
                var pm = GetComponentInChildren<cowsins.PlayerMovement>(true);
                if (pm != null) playerTransform = pm.transform;
            }
        }

        private void LateUpdate()
        {
            if (playerTransform == null) return;

            if (IsOwner)
            {
                // Root follows the moving player (this is what NetworkTransform replicates).
                transform.position = playerTransform.position;
                if (syncRotation) transform.rotation = playerTransform.rotation;
            }
            else
            {
                // Remote: the visual/physics child follows the networked root.
                playerTransform.position = transform.position;
                if (syncRotation) playerTransform.rotation = transform.rotation;
            }
        }
    }
}

