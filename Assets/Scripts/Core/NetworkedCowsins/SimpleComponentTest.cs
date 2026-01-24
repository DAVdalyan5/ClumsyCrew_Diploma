using UnityEngine;
using Unity.Netcode.Components;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Simple test to verify components can be instantiated without exceptions
    /// Attach to empty GameObject and run in editor
    /// </summary>
    public class SimpleComponentTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = true;
        
        [Header("Test Results")]
        [SerializeField] private string testResult = "Not run";
        
        private void Start()
        {
            if (runTestOnStart)
            {
                RunSimpleTest();
            }
        }
        
        [ContextMenu("Run Simple Test")]
        public void RunSimpleTest()
        {
            Debug.Log("=== Simple Component Test ===");
            
            try
            {
                // Test 1: Create GameObject with NetworkedCowsinsPlayerController
                GameObject testObj = new GameObject("TestPlayer");
                
                // Add required components
                testObj.AddComponent<Unity.Netcode.NetworkObject>();
                var playerMovement = testObj.AddComponent<cowsins.PlayerMovement>();
                var inputManager = testObj.AddComponent<cowsins.InputManager>();
                var networkedInputManager = testObj.AddComponent<NetworkedCowsinsInputManager>();
                var playerController = testObj.AddComponent<NetworkedCowsinsPlayerController>();
                
                Debug.Log("✓ Successfully created all components");
                
                // Test 2: Verify component references
                if (playerController.GetPlayerMovement() != null)
                    Debug.Log("✓ PlayerMovement reference valid");
                else
                    Debug.Log("✗ PlayerMovement reference null");
                
                if (playerController.GetNetworkedInputManager() != null)
                    Debug.Log("✓ NetworkedInputManager reference valid");
                else
                    Debug.Log("✗ NetworkedInputManager reference null");
                
                if (playerController.GetInputManager() != null)
                    Debug.Log("✓ InputManager reference valid");
                else
                    Debug.Log("✗ InputManager reference null");
                
                // Test 3: Test NetworkedCowsinsStateSync
                var stateSync = testObj.AddComponent<NetworkedCowsinsStateSync>();
                if (stateSync != null)
                    Debug.Log("✓ NetworkedCowsinsStateSync created successfully");
                
                // Cleanup
                DestroyImmediate(testObj);
                
                testResult = "PASS - All components created without exceptions";
                Debug.Log("=== Test PASSED ===");
            }
            catch (System.Exception e)
            {
                testResult = $"FAIL - Exception: {e.Message}";
                Debug.LogError($"=== Test FAILED: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }
        
        [ContextMenu("Check Component Dependencies")]
        public void CheckDependencies()
        {
            Debug.Log("=== Component Dependency Check ===");
            
            // Check if cowsins namespace is available
            System.Type playerMovementType = System.Type.GetType("cowsins.PlayerMovement, Assembly-CSharp");
            System.Type inputManagerType = System.Type.GetType("cowsins.InputManager, Assembly-CSharp");
            
            Debug.Log($"cowsins.PlayerMovement type: {(playerMovementType != null ? "✓ Found" : "✗ Missing")}");
            Debug.Log($"cowsins.InputManager type: {(inputManagerType != null ? "✓ Found" : "✗ Missing")}");
            
            // Check our components
            System.Type ourPlayerControllerType = typeof(NetworkedCowsinsPlayerController);
            System.Type ourInputManagerType = typeof(NetworkedCowsinsInputManager);
            System.Type ourStateSyncType = typeof(NetworkedCowsinsStateSync);
            
            Debug.Log($"NetworkedCowsinsPlayerController type: {(ourPlayerControllerType != null ? "✓ Found" : "✗ Missing")}");
            Debug.Log($"NetworkedCowsinsInputManager type: {(ourInputManagerType != null ? "✓ Found" : "✗ Missing")}");
            Debug.Log($"NetworkedCowsinsStateSync type: {(ourStateSyncType != null ? "✓ Found" : "✗ Missing")}");
            
            // Check Unity Netcode types
            System.Type networkObjectType = typeof(Unity.Netcode.NetworkObject);
            System.Type networkTransformType = typeof(Unity.Netcode.Components.NetworkTransform);
            
            Debug.Log($"NetworkObject type: {(networkObjectType != null ? "✓ Found" : "✗ Missing")}");
            Debug.Log($"NetworkTransform type: {(networkTransformType != null ? "✓ Found" : "✗ Missing")}");
            
            Debug.Log("=== Dependency Check Complete ===");
        }
    }
}