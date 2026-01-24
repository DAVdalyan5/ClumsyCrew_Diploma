using cowsins;
using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Network-aware version of Cowsins InputManager.
    /// Only processes input for owner clients in multiplayer.
    /// </summary>
    public class NetworkedCowsinsInputManager : MonoBehaviour
    {
        [Header("Ownership Source (required in multiplayer)")]
        [Tooltip("Reference to the player's NetworkBehaviour (e.g., NetworkedCowsinsPlayerController) used to decide IsOwner.\n" +
                 "Because Cowsins keeps InputManager on a separate GameObject, this must be assigned.")]
        [SerializeField] private NetworkBehaviour ownerAuthority;

        private InputManager _inputManager;
        private bool _isInitialized = false;
        
        private void Awake()
        {
            _inputManager = GetComponent<InputManager>();
            
            if (_inputManager == null)
            {
                Debug.LogError("[NetworkedCowsinsInputManager] No InputManager component found!");
                enabled = false;
                return;
            }

            ApplyOwnershipState();
        }
        
        private void OnEnable()
        {
            if (_inputManager == null) return;

            ApplyOwnershipState();
        }
        
        private void OnDisable()
        {
            if (_inputManager == null) return;
            
            // Only disable if we were initialized
            if (_isInitialized)
            {
                _inputManager.enabled = false;
                _isInitialized = false;
            }
        }
        
        private void Update()
        {
            // Keep enforcing ownership while running (handles late spawn / ownership changes)
            ApplyOwnershipState();
        }
        
        /// <summary>
        /// Check if this input manager is active for the current client
        /// </summary>
        public bool IsActiveForClient()
        {
            return IsOwnerOrStandalone();
        }
        
        /// <summary>
        /// Force disable input processing (e.g., when player is dead or menu is open)
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            if (!IsOwnerOrStandalone())
            {
                // Non-owners should never have input enabled
                if (_inputManager != null) _inputManager.enabled = false;
                this.enabled = false;
                return;
            }
            
            this.enabled = enabled;
            
            if (_inputManager != null)
            {
                _inputManager.enabled = enabled;
                
                if (enabled && !_isInitialized)
                {
                    _isInitialized = true;
                }
                else if (!enabled && _isInitialized)
                {
                    _isInitialized = false;
                }
            }
        }
        
        /// <summary>
        /// Get the underlying InputManager component
        /// </summary>
        public InputManager GetInputManager() => _inputManager;

        public void SetOwnerAuthority(NetworkBehaviour authority)
        {
            ownerAuthority = authority;
            ApplyOwnershipState();
        }

        private bool IsOwnerOrStandalone()
        {
            // Standalone: no netcode running
            if (NetworkManager.Singleton == null) return true;

            // Multiplayer: must have an authority reference, otherwise be safe and disable input.
            if (ownerAuthority == null || ownerAuthority.NetworkObject == null) return false;

            return ownerAuthority.IsOwner;
        }

        private void ApplyOwnershipState()
        {
            if (_inputManager == null) return;

            bool shouldBeActive = IsOwnerOrStandalone();

            if (shouldBeActive)
            {
                if (!_inputManager.enabled) _inputManager.enabled = true;
                _isInitialized = true;
                if (!enabled) enabled = true;
            }
            else
            {
                if (_inputManager.enabled) _inputManager.enabled = false;
                _isInitialized = false;
                if (enabled) enabled = true; // keep this component enabled so it can re-evaluate later
            }
        }
    }
}