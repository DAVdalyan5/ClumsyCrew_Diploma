using cowsins;
using Easy.MessageHub;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Network-aware wrapper for Cowsins FPS Controller.
    /// Handles ownership-based behavior for networked multiplayer.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedCowsinsPlayerController : NetworkBehaviour
    {
        [Header("Cowsins Components")]
        [Tooltip("Reference to the Cowsins PlayerMovement (usually on the 'Player' child).")]
        [SerializeField] private PlayerMovement playerMovement;
        [Tooltip("Reference to the NetworkedCowsinsInputManager component.\n" +
                 "In Cowsins prefabs InputManager is usually on a separate child GameObject, so assign it from there.")]
        [SerializeField] private NetworkedCowsinsInputManager networkedInputManager;

        [Header("Hierarchy")]
        [Tooltip("The transform that actually moves (usually the 'Player' child). If null, we use PlayerMovement.transform.")]
        [SerializeField] private Transform movingTransform;

        [Header("Owner-only Objects (disable for non-owners)")]
        [Tooltip("These GameObjects will be enabled only on the owning client (camera, UI, local-only audio, etc).")]
        [SerializeField] private GameObject[] ownerOnlyObjects;
        
        [Header("Network Settings")]
        [Tooltip("Enable input processing only for owner client")]
        [SerializeField] private bool ownerOnlyInput = true;
        
        [Tooltip("Enable movement physics only for owner client")]
        [SerializeField] private bool ownerOnlyPhysics = true;
        
        private IMessageHub _messageHub;
        
        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            Debug.Log($"[NetworkedCowsinsPlayerController] Spawned for client {OwnerClientId}, IsOwner: {IsOwner}, IsServer: {IsServer}");
            
            if (playerMovement == null)
                playerMovement = GetComponentInChildren<PlayerMovement>(true);

            if (playerMovement == null)
                Debug.LogError("[NetworkedCowsinsPlayerController] Missing PlayerMovement reference (expected on 'Player' child).");
                
            // If input manager wrapper is on a sibling object, user must assign it.
            // We try a best-effort search in parents/children, but avoid guessing too much.
            if (networkedInputManager == null)
            {
                networkedInputManager = GetComponentInChildren<NetworkedCowsinsInputManager>(true);
                if (networkedInputManager == null)
                {
                    networkedInputManager = GetComponentInParent<NetworkedCowsinsInputManager>();
                }
            }

            if (movingTransform == null && playerMovement != null)
                movingTransform = playerMovement.transform;
            
            SetupOwnershipBehavior();
        }
        
        private void SetupOwnershipBehavior()
        {
            ApplyOwnerOnlyObjects();

            if (!IsOwner)
            {
                // Non-owner clients: disable input processing
                if (ownerOnlyInput && networkedInputManager != null)
                {
                    Debug.Log($"[NetworkedCowsinsPlayerController] Disabling input manager for non-owner client");
                    networkedInputManager.SetInputEnabled(false);
                }

                DisableNonOwnerLocalSystems();
                
                // Non-owner clients: disable physics if configured
                if (ownerOnlyPhysics && playerMovement != null)
                {
                    var rb = playerMovement.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        Debug.Log($"[NetworkedCowsinsPlayerController] Setting Rigidbody to kinematic for non-owner client");
                    }
                }

                // Non-owner: disable PlayerMovement to avoid local simulation fighting networked transform
                if (playerMovement != null) playerMovement.enabled = false;
            }
            else
            {
                // Owner client: ensure everything is enabled
                if (networkedInputManager != null)
                {
                    networkedInputManager.SetOwnerAuthority(this);
                    networkedInputManager.SetInputEnabled(true);
                    Debug.Log($"[NetworkedCowsinsPlayerController] Input manager enabled for owner client");
                }
                
                if (playerMovement != null)
                {
                    var rb = playerMovement.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        Debug.Log($"[NetworkedCowsinsPlayerController] Rigidbody set to non-kinematic for owner client");
                    }
                }

                playerMovement.enabled = true;
            }
        }

        private void DisableNonOwnerLocalSystems()
        {
            // These systems expect local camera/input and will NRE on remote instances.
            var camFx = GetComponentsInChildren<cowsins.CameraEffects>(true);
            var states = GetComponentsInChildren<cowsins.PlayerStates>(true);
            var interact = GetComponentsInChildren<cowsins.InteractManager>(true);
            var pause = GetComponentsInChildren<cowsins.PauseMenu>(true);

            foreach (var c in camFx) if (c != null) c.enabled = false;
            foreach (var c in states) if (c != null) c.enabled = false;
            foreach (var c in interact) if (c != null) c.enabled = false;
            foreach (var c in pause) if (c != null) c.enabled = false;
        }

        private void ApplyOwnerOnlyObjects()
        {
            if (ownerOnlyObjects == null) return;

            bool active = IsOwner;
            foreach (var go in ownerOnlyObjects)
            {
                if (go == null) continue;
                go.SetActive(active);
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Debug.Log($"[NetworkedCowsinsPlayerController] Despawned for client {OwnerClientId}");
        }
        
        /// <summary>
        /// Get the Cowsins PlayerMovement component
        /// </summary>
        public PlayerMovement GetPlayerMovement() => playerMovement;
        
        /// <summary>
        /// Get the NetworkedCowsinsInputManager component
        /// </summary>
        public NetworkedCowsinsInputManager GetNetworkedInputManager() => networkedInputManager;
        
        /// <summary>
        /// Get the underlying InputManager component
        /// </summary>
        public InputManager GetInputManager() => networkedInputManager?.GetInputManager();

        public Transform GetMovingTransform() => movingTransform != null ? movingTransform : transform;
        
        /// <summary>
        /// Teleport player to position (server-authoritative)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TeleportPlayerServerRpc(Vector3 position, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            
            Debug.Log($"[NetworkedCowsinsPlayerController] Teleporting player to {position}");
            
            // Move the player on server
            transform.position = position;
            
            // Sync to all clients
            TeleportPlayerClientRpc(position);
        }
        
        [ClientRpc]
        private void TeleportPlayerClientRpc(Vector3 position)
        {
            if (IsOwner) return; // Owner already moved by server RPC
            
            transform.position = position;
            Debug.Log($"[NetworkedCowsinsPlayerController] Client teleported to {position}");
        }
        
        /// <summary>
        /// Apply force to player (server-authoritative)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ApplyForceServerRpc(Vector3 force, ForceMode forceMode = ForceMode.Force, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(force, forceMode);
                Debug.Log($"[NetworkedCowsinsPlayerController] Applying force {force} with mode {forceMode}");
                
                // Sync force to all clients
                ApplyForceClientRpc(force, forceMode);
            }
        }
        
        [ClientRpc]
        private void ApplyForceClientRpc(Vector3 force, ForceMode forceMode)
        {
            if (IsOwner) return; // Owner already had force applied by server
            
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(force, forceMode);
            }
        }
    }
}