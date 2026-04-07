using cowsins;
using Easy.MessageHub;
using HeistNSeek.Core;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

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
        [Tooltip("These GameObjects will be enabled only on the owning client (camera, UI, local-only audio, etc). " +
                 "The Cowsins camera rig includes the only AudioListener per client; Main scene uses SceneFallbackAudioListener until the local player spawns.")]
        [SerializeField] private GameObject[] ownerOnlyObjects;

        [Header("First-person visibility (hide for local player)")]
        [Tooltip("These GameObjects (e.g. PlayerGraphics full body) are hidden for the local player so you see only arms/weapons. Other players still see your full body.")]
        [SerializeField] private GameObject[] localPlayerHiddenObjects;
        
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
            EnsureInjectedDependencies();
            
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

            if (IsOwner)
                WirePusherCamera();

            SetupOwnershipBehavior();

            if (IsOwner && PoolManager.Instance == null)
                Debug.LogError("[NetworkedCowsinsPlayerController] PoolManager.Instance is null. Assign CowsinsSessionServices prefab on GameplayLifetimeScope so session services spawn before players.");
        }

        private void EnsureInjectedDependencies()
        {
            if (_messageHub != null)
                return;

            LifetimeScope scope = FindAnyObjectByType<GameplayLifetimeScope>();
            if (scope == null)
                scope = FindAnyObjectByType<LifetimeScope>();

            if (scope != null && scope.Container != null)
                scope.Container.InjectGameObject(gameObject);
        }

        private void WirePusherCamera()
        {
            var pusher = GetComponentInChildren<CharacterPusher>(true);
            if (pusher == null) return;

            Camera mainCam = null;
            var weaponController = playerMovement != null ? playerMovement.GetComponent<WeaponController>() : null;
            if (weaponController != null && weaponController.MainCamera != null)
                mainCam = weaponController.MainCamera;
            if (mainCam == null)
            {
                var camTransform = FindChildByName(transform, "Camera");
                if (camTransform != null)
                    mainCam = camTransform.GetComponentInChildren<Camera>(true);
            }
            if (mainCam != null)
                pusher.CameraTransform = mainCam.transform;
        }

        private static Transform FindChildByName(Transform parent, string name)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
                var found = FindChildByName(child, name);
                if (found != null) return found;
            }
            return null;
        }
        
        private void SetupOwnershipBehavior()
        {
            ApplyOwnerOnlyObjects();
            ApplyLocalPlayerVisibility();

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

        /// <summary>
        /// Hides full body (PlayerGraphics) for local player so they see only arms/weapons.
        /// Remote players still see the full body.
        /// </summary>
        private void ApplyLocalPlayerVisibility()
        {
            if (localPlayerHiddenObjects == null) return;

            // For owner: hide body so we see only arms. For non-owner: keep visible (others see our body).
            bool hideFromSelf = IsOwner;
            foreach (var go in localPlayerHiddenObjects)
            {
                if (go == null) continue;
                go.SetActive(!hideFromSelf);
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

        private Rigidbody GetMovingRigidbody()
        {
            var moving = GetMovingTransform();
            return moving != null ? moving.GetComponent<Rigidbody>() : null;
        }

        /// <summary>
        /// Called by the server when this player is pushed by another (e.g. CharacterPusher).
        /// Applies impulse to the Player child's Rigidbody and replicates to clients.
        /// </summary>
        public void ReceivePushFromServer(Vector3 forceVector)
        {
            if (!IsServer) return;

            var rb = GetMovingRigidbody();
            if (rb != null)
            {
                rb.AddForce(forceVector, ForceMode.Impulse);
                ReceivePushClientRpc(forceVector);
            }
        }

        [ClientRpc]
        private void ReceivePushClientRpc(Vector3 forceVector)
        {
            if (IsOwner) return;

            var rb = GetMovingRigidbody();
            if (rb != null)
                rb.AddForce(forceVector, ForceMode.Impulse);
        }

        /// <summary>
        /// Server RPC for self-applied stumble force (e.g. from CowsinsCollisionPushHandler when high-speed impact is detected).
        /// </summary>
        [ServerRpc]
        public void RequestApplyStumbleForceServerRpc(Vector3 forceVector)
        {
            if (!IsServer) return;
            ReceivePushFromServer(forceVector);
        }

        /// <summary>
        /// Teleport player to position (server-authoritative)
        /// </summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void TeleportPlayerServerRpc(Vector3 position)
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
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ApplyForceServerRpc(Vector3 force, ForceMode forceMode = ForceMode.Force)
        {
            if (!IsServer) return;
            
            var rb = GetMovingRigidbody();
            if (rb != null)
            {
                rb.AddForce(force, forceMode);
                Debug.Log($"[NetworkedCowsinsPlayerController] Applying force {force} with mode {forceMode}");
                ApplyForceClientRpc(force, forceMode);
            }
        }

        [ClientRpc]
        private void ApplyForceClientRpc(Vector3 force, ForceMode forceMode)
        {
            if (IsOwner) return;

            var rb = GetMovingRigidbody();
            if (rb != null)
                rb.AddForce(force, forceMode);
        }

    }
}