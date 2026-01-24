using cowsins;
using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Synchronizes Cowsins player states across the network
    /// </summary>
    [DisallowMultipleComponent]
    public class NetworkedCowsinsStateSync : NetworkBehaviour
    {
        [Header("State Synchronization")]
        [Tooltip("Sync player grounded state")]
        [SerializeField] private bool syncGroundedState = true;
        
        [Tooltip("Sync player crouching state")]
        [SerializeField] private bool syncCrouchingState = true;
        
        [Tooltip("Sync player sliding state")]
        [SerializeField] private bool syncSlidingState = true;
        
        [Tooltip("Sync player climbing state")]
        [SerializeField] private bool syncClimbingState = true;
        
        [Tooltip("Sync player wall running state")]
        [SerializeField] private bool syncWallRunningState = true;
        
        [Tooltip("Sync player dashing state")]
        [SerializeField] private bool syncDashingState = true;
        
        [Header("Sync Rates")]
        [Tooltip("How often to sync states (times per second)")]
        [SerializeField] private float syncRate = 10f;
        
        [Header("References")]
        [Tooltip("Cowsins PlayerMovement (usually on the 'Player' child). If null we auto-find in children.")]
        [SerializeField] private PlayerMovement playerMovement;

        private PlayerMovement _playerMovement;
        private float _lastSyncTime;
        
        // Network variables for state synchronization
        private NetworkVariable<bool> _networkGrounded = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
        
        private NetworkVariable<bool> _networkCrouching = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
        
        private NetworkVariable<bool> _networkSliding = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
        
        private NetworkVariable<bool> _networkClimbing = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
        
        private NetworkVariable<bool> _networkWallRunning = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
        
        private NetworkVariable<bool> _networkDashing = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
        
        // Local cache for comparison
        private bool _lastGrounded;
        private bool _lastCrouching;
        private bool _lastSliding;
        private bool _lastClimbing;
        private bool _lastWallRunning;
        private bool _lastDashing;
        
        private void Awake()
        {
            _playerMovement = playerMovement != null ? playerMovement : GetComponentInChildren<PlayerMovement>(true);
            if (playerMovement == null) playerMovement = _playerMovement;
            
            if (_playerMovement == null)
            {
                Debug.LogError("[NetworkedCowsinsStateSync] PlayerMovement component not found!");
                enabled = false;
                return;
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (!IsOwner)
            {
                // Subscribe to network variable changes for non-owners
                _networkGrounded.OnValueChanged += OnGroundedChanged;
                _networkCrouching.OnValueChanged += OnCrouchingChanged;
                _networkSliding.OnValueChanged += OnSlidingChanged;
                _networkClimbing.OnValueChanged += OnClimbingChanged;
                _networkWallRunning.OnValueChanged += OnWallRunningChanged;
                _networkDashing.OnValueChanged += OnDashingChanged;
                
                // Apply initial values
                ApplyNetworkedStates();
            }
            
            Debug.Log($"[NetworkedCowsinsStateSync] Spawned for client {OwnerClientId}, IsOwner: {IsOwner}");
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            if (!IsOwner)
            {
                // Unsubscribe from network variable changes
                _networkGrounded.OnValueChanged -= OnGroundedChanged;
                _networkCrouching.OnValueChanged -= OnCrouchingChanged;
                _networkSliding.OnValueChanged -= OnSlidingChanged;
                _networkClimbing.OnValueChanged -= OnClimbingChanged;
                _networkWallRunning.OnValueChanged -= OnWallRunningChanged;
                _networkDashing.OnValueChanged -= OnDashingChanged;
            }
        }
        
        private void Update()
        {
            if (!IsOwner) return;
            
            // Sync at specified rate
            if (Time.time - _lastSyncTime < 1f / syncRate) return;
            
            SyncStates();
            _lastSyncTime = Time.time;
        }
        
        private void SyncStates()
        {
            if (_playerMovement == null) return;
            
            // Only sync if state changed
            if (syncGroundedState && _playerMovement.Grounded != _lastGrounded)
            {
                _networkGrounded.Value = _playerMovement.Grounded;
                _lastGrounded = _playerMovement.Grounded;
            }
            
            if (syncCrouchingState && _playerMovement.IsCrouching != _lastCrouching)
            {
                _networkCrouching.Value = _playerMovement.IsCrouching;
                _lastCrouching = _playerMovement.IsCrouching;
            }
            
            if (syncSlidingState && _playerMovement.IsSliding != _lastSliding)
            {
                _networkSliding.Value = _playerMovement.IsSliding;
                _lastSliding = _playerMovement.IsSliding;
            }
            
            if (syncClimbingState && _playerMovement.IsClimbing != _lastClimbing)
            {
                _networkClimbing.Value = _playerMovement.IsClimbing;
                _lastClimbing = _playerMovement.IsClimbing;
            }
            
            if (syncWallRunningState && _playerMovement.IsWallRunning != _lastWallRunning)
            {
                _networkWallRunning.Value = _playerMovement.IsWallRunning;
                _lastWallRunning = _playerMovement.IsWallRunning;
            }
            
            if (syncDashingState && _playerMovement.IsDashing != _lastDashing)
            {
                _networkDashing.Value = _playerMovement.IsDashing;
                _lastDashing = _playerMovement.IsDashing;
            }
        }
        
        private void ApplyNetworkedStates()
        {
            if (_playerMovement == null) return;
            
            // Apply networked states to local player movement
            // Note: Some states might be read-only in Cowsins, so we apply what we can
            _playerMovement.Grounded = _networkGrounded.Value;
            
            // For states that affect visual/audio, we might need to trigger events
            // This depends on how Cowsins handles state changes
        }
        
        // Network variable change handlers
        private void OnGroundedChanged(bool oldValue, bool newValue)
        {
            if (_playerMovement != null)
            {
                _playerMovement.Grounded = newValue;
                Debug.Log($"[NetworkedCowsinsStateSync] Grounded changed: {newValue}");
            }
        }
        
        private void OnCrouchingChanged(bool oldValue, bool newValue)
        {
            if (_playerMovement != null)
            {
                _playerMovement.IsCrouching = newValue;
                Debug.Log($"[NetworkedCowsinsStateSync] Crouching changed: {newValue}");
            }
        }
        
        private void OnSlidingChanged(bool oldValue, bool newValue)
        {
            if (_playerMovement != null)
            {
                _playerMovement.IsSliding = newValue;
                Debug.Log($"[NetworkedCowsinsStateSync] Sliding changed: {newValue}");
            }
        }
        
        private void OnClimbingChanged(bool oldValue, bool newValue)
        {
            if (_playerMovement != null)
            {
                _playerMovement.IsClimbing = newValue;
                Debug.Log($"[NetworkedCowsinsStateSync] Climbing changed: {newValue}");
            }
        }
        
        private void OnWallRunningChanged(bool oldValue, bool newValue)
        {
            if (_playerMovement != null)
            {
                _playerMovement.IsWallRunning = newValue;
                Debug.Log($"[NetworkedCowsinsStateSync] WallRunning changed: {newValue}");
            }
        }
        
        private void OnDashingChanged(bool oldValue, bool newValue)
        {
            if (_playerMovement != null)
            {
                _playerMovement.IsDashing = newValue;
                Debug.Log($"[NetworkedCowsinsStateSync] Dashing changed: {newValue}");
            }
        }
        
        /// <summary>
        /// Get current networked grounded state
        /// </summary>
        public bool GetNetworkedGrounded() => _networkGrounded.Value;
        
        /// <summary>
        /// Get current networked crouching state
        /// </summary>
        public bool GetNetworkedCrouching() => _networkCrouching.Value;
        
        /// <summary>
        /// Get current networked sliding state
        /// </summary>
        public bool GetNetworkedSliding() => _networkSliding.Value;
        
        /// <summary>
        /// Get current networked climbing state
        /// </summary>
        public bool GetNetworkedClimbing() => _networkClimbing.Value;
        
        /// <summary>
        /// Get current networked wall running state
        /// </summary>
        public bool GetNetworkedWallRunning() => _networkWallRunning.Value;
        
        /// <summary>
        /// Get current networked dashing state
        /// </summary>
        public bool GetNetworkedDashing() => _networkDashing.Value;
        
        /// <summary>
        /// Force sync all states immediately
        /// </summary>
        public void ForceSync()
        {
            if (!IsOwner) return;
            
            SyncStates();
            _lastSyncTime = Time.time;
        }
    }
}