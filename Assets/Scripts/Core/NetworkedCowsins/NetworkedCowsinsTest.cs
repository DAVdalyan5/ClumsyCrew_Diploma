using cowsins;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Test script for verifying networked Cowsins functionality
    /// Attach to a GameObject in scene to run tests
    /// </summary>
    public class NetworkedCowsinsTest : NetworkBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runAutomatedTests = true;
        [SerializeField] private float testInterval = 5f;
        
        [Header("Test Prefab")]
        [SerializeField] private GameObject testPlayerPrefab;
        
        private float _lastTestTime;
        private GameObject _testPlayerInstance;
        
        private void Start()
        {
            if (runAutomatedTests && IsServer)
            {
                Debug.Log("[NetworkedCowsinsTest] Automated tests enabled");
                _lastTestTime = Time.time;
            }
        }
        
        private void Update()
        {
            if (!runAutomatedTests || !IsServer) return;
            
            if (Time.time - _lastTestTime >= testInterval)
            {
                RunTestSuite();
                _lastTestTime = Time.time;
            }
        }
        
        /// <summary>
        /// Run all tests in the test suite
        /// </summary>
        [ContextMenu("Run Test Suite")]
        public void RunTestSuite()
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkedCowsinsTest] Tests can only be run on server");
                return;
            }
            
            Debug.Log("=== Networked Cowsins Test Suite ===");
            
            TestComponentPresence();
            TestOwnershipBehavior();
            TestNetworkSynchronization();
            
            Debug.Log("=== Test Suite Complete ===");
        }
        
        /// <summary>
        /// Test 1: Verify all required components are present
        /// </summary>
        private void TestComponentPresence()
        {
            Debug.Log("Test 1: Component Presence Check");
            
            if (testPlayerPrefab == null)
            {
                Debug.LogError("  ✗ Test player prefab not assigned");
                return;
            }
            
            // Check prefab components
            NetworkObject networkObject = testPlayerPrefab.GetComponent<NetworkObject>();
            NetworkTransform networkTransform = testPlayerPrefab.GetComponent<NetworkTransform>();
            NetworkedCowsinsPlayerController playerController = testPlayerPrefab.GetComponent<NetworkedCowsinsPlayerController>();
            NetworkedCowsinsInputManager networkedInputManager = testPlayerPrefab.GetComponent<NetworkedCowsinsInputManager>();
            NetworkedCowsinsStateSync stateSync = testPlayerPrefab.GetComponent<NetworkedCowsinsStateSync>();
            InputManager originalInputManager = testPlayerPrefab.GetComponent<InputManager>();
            
            Debug.Log($"  NetworkObject: {(networkObject != null ? "✓" : "✗")}");
            Debug.Log($"  NetworkTransform: {(networkTransform != null ? "✓" : "✗")}");
            Debug.Log($"  NetworkedCowsinsPlayerController: {(playerController != null ? "✓" : "✗")}");
            Debug.Log($"  NetworkedCowsinsInputManager: {(networkedInputManager != null ? "✓" : "✗")}");
            Debug.Log($"  NetworkedCowsinsStateSync: {(stateSync != null ? "✓" : "✗")}");
            Debug.Log($"  Original InputManager: {(originalInputManager != null ? "✓ (required)" : "✗ (missing)")}");
            
            if (networkedInputManager != null && originalInputManager == null)
            {
                Debug.LogWarning("  ⚠ NetworkedCowsinsInputManager requires original InputManager component!");
            }
        }
        
        /// <summary>
        /// Test 2: Test ownership behavior
        /// </summary>
        private void TestOwnershipBehavior()
        {
            Debug.Log("Test 2: Ownership Behavior");
            
            if (testPlayerPrefab == null) return;
            
            // Spawn a test player
            if (_testPlayerInstance == null)
            {
                _testPlayerInstance = Instantiate(testPlayerPrefab, Vector3.zero, Quaternion.identity);
                var networkObject = _testPlayerInstance.GetComponent<NetworkObject>();
                
                if (networkObject != null)
                {
                    networkObject.Spawn();
                    Debug.Log("  ✓ Test player spawned");
                }
                else
                {
                    Debug.LogError("  ✗ Failed to spawn test player - no NetworkObject");
                    return;
                }
            }
            
            // Check ownership
            var playerController = _testPlayerInstance.GetComponent<NetworkedCowsinsPlayerController>();
            if (playerController != null)
            {
                Debug.Log($"  Player OwnerClientId: {playerController.OwnerClientId}");
                Debug.Log($"  Player IsOwner: {playerController.IsOwner}");
                Debug.Log($"  Player IsServer: {playerController.IsServer}");
            }
        }
        
        /// <summary>
        /// Test 3: Test network synchronization
        /// </summary>
        private void TestNetworkSynchronization()
        {
            Debug.Log("Test 3: Network Synchronization");
            
            if (_testPlayerInstance == null) return;
            
            var networkTransform = _testPlayerInstance.GetComponent<NetworkTransform>();
            if (networkTransform != null)
            {
                Debug.Log($"  NetworkTransform Interpolate: {networkTransform.Interpolate}");
                Debug.Log($"  Sync Position: X={networkTransform.SyncPositionX}, Y={networkTransform.SyncPositionY}, Z={networkTransform.SyncPositionZ}");
                Debug.Log($"  Sync Rotation: X={networkTransform.SyncRotAngleX}, Y={networkTransform.SyncRotAngleY}, Z={networkTransform.SyncRotAngleZ}");
            }
            
            var stateSync = _testPlayerInstance.GetComponent<NetworkedCowsinsStateSync>();
            if (stateSync != null)
            {
                Debug.Log($"  State Sync Rate: {stateSync.GetNetworkedGrounded()}");
                Debug.Log($"  Grounded: {stateSync.GetNetworkedGrounded()}");
                Debug.Log($"  Crouching: {stateSync.GetNetworkedCrouching()}");
            }
        }
        
        /// <summary>
        /// Clean up test objects
        /// </summary>
        [ContextMenu("Cleanup Test Objects")]
        public void CleanupTestObjects()
        {
            if (_testPlayerInstance != null)
            {
                var networkObject = _testPlayerInstance.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                }
                
                Destroy(_testPlayerInstance);
                _testPlayerInstance = null;
                Debug.Log("[NetworkedCowsinsTest] Test objects cleaned up");
            }
        }
        
        /// <summary>
        /// Manual test: Spawn test player
        /// </summary>
        [ContextMenu("Spawn Test Player")]
        public void SpawnTestPlayer()
        {
            if (!IsServer)
            {
                Debug.LogWarning("Can only spawn test players on server");
                return;
            }
            
            if (testPlayerPrefab == null)
            {
                Debug.LogError("Test player prefab not assigned");
                return;
            }
            
            CleanupTestObjects();
            
            Vector3 spawnPosition = new Vector3(0, 2, 0);
            _testPlayerInstance = Instantiate(testPlayerPrefab, spawnPosition, Quaternion.identity);
            var networkObject = _testPlayerInstance.GetComponent<NetworkObject>();
            
            if (networkObject != null)
            {
                networkObject.Spawn();
                Debug.Log($"Test player spawned at {spawnPosition}");
            }
            else
            {
                Debug.LogError("Failed to spawn test player - no NetworkObject");
                Destroy(_testPlayerInstance);
                _testPlayerInstance = null;
            }
        }
        
        /// <summary>
        /// Manual test: Teleport test player
        /// </summary>
        [ContextMenu("Teleport Test Player")]
        public void TeleportTestPlayer()
        {
            if (_testPlayerInstance == null)
            {
                Debug.LogWarning("No test player to teleport");
                return;
            }
            
            var playerController = _testPlayerInstance.GetComponent<NetworkedCowsinsPlayerController>();
            if (playerController != null)
            {
                Vector3 newPosition = new Vector3(Random.Range(-5f, 5f), 2, Random.Range(-5f, 5f));
                playerController.TeleportPlayerServerRpc(newPosition);
                Debug.Log($"Teleporting test player to {newPosition}");
            }
        }
        
        /// <summary>
        /// Manual test: Apply force to test player
        /// </summary>
        [ContextMenu("Apply Force to Test Player")]
        public void ApplyForceToTestPlayer()
        {
            if (_testPlayerInstance == null)
            {
                Debug.LogWarning("No test player to apply force to");
                return;
            }
            
            var playerController = _testPlayerInstance.GetComponent<NetworkedCowsinsPlayerController>();
            if (playerController != null)
            {
                Vector3 force = new Vector3(Random.Range(-100f, 100f), 200f, Random.Range(-100f, 100f));
                playerController.ApplyForceServerRpc(force, UnityEngine.ForceMode.Impulse);
                Debug.Log($"Applying force {force} to test player");
            }
        }
        
        public override void OnDestroy()
        {
            CleanupTestObjects();
            base.OnDestroy();
        }
    }
}