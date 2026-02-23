using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using cowsins;
using Unity.Netcode.Components;
using Object = UnityEngine.Object;

namespace HeistNSeek.Core.NetworkedCowsins.Editor
{
#if UNITY_EDITOR
    /// <summary>
    /// Editor utility to help set up networked Cowsins prefabs
    /// </summary>
    public static class NetworkedCowsinsSetup
    {
        [MenuItem("Tools/Networked Cowsins/Create Networked Player Prefab")]
        public static void CreateNetworkedPlayerPrefab()
        {
            // Find the original prefab
            string originalPrefabPath = "Assets/ThirdParty/Cowsins/Prefabs/PlayerControllers/MovementCowsinsFPSController.prefab";
            GameObject originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(originalPrefabPath);
            
            if (originalPrefab == null)
            {
                Debug.LogError($"Original prefab not found at: {originalPrefabPath}");
                EditorUtility.DisplayDialog("Error", $"Original prefab not found at: {originalPrefabPath}", "OK");
                return;
            }
            
            // Create a copy
            string newPrefabPath = "Assets/Prefabs/Networked/NetworkedMovementCowsinsFPSController.prefab";
            
            // Check if prefab already exists
            if (AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog("Prefab Exists", 
                    $"Prefab already exists at: {newPrefabPath}\nDo you want to overwrite it?", 
                    "Overwrite", "Cancel");
                
                if (!overwrite) return;
            }
            
            // Create the prefab
            GameObject prefabInstance = PrefabUtility.InstantiatePrefab(originalPrefab) as GameObject;
            
            if (prefabInstance == null)
            {
                Debug.LogError("Failed to instantiate prefab");
                return;
            }
            
            try
            {
                // Add NetworkObject if not present
                NetworkObject networkObject = prefabInstance.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    networkObject = prefabInstance.AddComponent<NetworkObject>();
                    Debug.Log("Added NetworkObject component");
                }
                
                // Add NetworkTransform if not present
                NetworkTransform networkTransform = prefabInstance.GetComponent<NetworkTransform>();
                if (networkTransform == null)
                {
                    networkTransform = prefabInstance.AddComponent<NetworkTransform>();
                    networkTransform.Interpolate = true;
                    networkTransform.SyncPositionX = true;
                    networkTransform.SyncPositionY = true;
                    networkTransform.SyncPositionZ = true;
                    networkTransform.SyncRotAngleX = true;
                    networkTransform.SyncRotAngleY = true;
                    networkTransform.SyncRotAngleZ = true;
                    networkTransform.SyncScaleX = false;
                    networkTransform.SyncScaleY = false;
                    networkTransform.SyncScaleZ = false;
                    Debug.Log("Added NetworkTransform component");
                }
                
                // Add NetworkedCowsinsPlayerController
                NetworkedCowsinsPlayerController playerController = prefabInstance.GetComponent<NetworkedCowsinsPlayerController>();
                if (playerController == null)
                {
                    playerController = prefabInstance.AddComponent<NetworkedCowsinsPlayerController>();
                    
                    // Try to auto-assign references
                    PlayerMovement playerMovement = prefabInstance.GetComponent<PlayerMovement>();
                    InputManager inputManager = prefabInstance.GetComponent<InputManager>();
                    
                    // SerializedObject for setting serialized fields
                    SerializedObject serializedPlayerController = new SerializedObject(playerController);
                    serializedPlayerController.FindProperty("playerMovement").objectReferenceValue = playerMovement;
                    serializedPlayerController.FindProperty("inputManager").objectReferenceValue = inputManager;
                    serializedPlayerController.ApplyModifiedProperties();
                    
                    Debug.Log("Added NetworkedCowsinsPlayerController component");
                }
                
                // Add NetworkedCowsinsInputManager (doesn't replace, works alongside)
                NetworkedCowsinsInputManager networkedInputManager = prefabInstance.GetComponent<NetworkedCowsinsInputManager>();
                if (networkedInputManager == null)
                {
                    networkedInputManager = prefabInstance.AddComponent<NetworkedCowsinsInputManager>();
                    Debug.Log("Added NetworkedCowsinsInputManager component");
                }
                
                // Save as new prefab
                GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(prefabInstance, newPrefabPath);
                
                if (newPrefab != null)
                {
                    Debug.Log($"Successfully created networked prefab at: {newPrefabPath}");
                    EditorUtility.DisplayDialog("Success", $"Networked prefab created at:\n{newPrefabPath}", "OK");
                    
                    // Select the new prefab in the Project window
                    Selection.activeObject = newPrefab;
                    EditorGUIUtility.PingObject(newPrefab);
                }
                else
                {
                    Debug.LogError("Failed to save prefab");
                    EditorUtility.DisplayDialog("Error", "Failed to save prefab", "OK");
                }
            }
            finally
            {
                // Clean up
                Object.DestroyImmediate(prefabInstance);
            }
        }

        [MenuItem("Tools/Networked Cowsins/Setup Selected Cowsins Player Prefab")]
        public static void SetupSelectedCowsinsPlayerPrefab()
        {
            var selected = Selection.activeObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Networked Cowsins", "Select your player prefab in the Project window first.", "OK");
                return;
            }

            var path = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Networked Cowsins", "Selected asset is not a prefab.", "OK");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // Validate + cleanup first (matches runtime safety net, but we want the prefab correct).
                CleanupNestedNetworking(root);
                SetupPrefabContents(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[NetworkedCowsinsSetup] Setup complete: {path}");
                EditorUtility.DisplayDialog("Networked Cowsins", "Prefab setup complete.\n\nNext: add this prefab to NetworkManager's Network Prefabs list and set PlayerSpawner.playerPrefab.", "OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupPrefabContents(GameObject prefabRoot)
        {
            // Expected hierarchy (from your screenshot):
            // NetworkedMovementCowsinsFPSController
            //  - Player (has PlayerMovement + Rigidbody)
            //  - Camera
            //  - PlayerGraphics
            //  - GeneralManagers
            //  - PlayerUI
            //  - InputManager (has cowsins.InputManager)

            // Root is spawned by PlayerSpawner -> keep NetworkObject ONLY on root to avoid Netcode reparent-before-spawn exceptions.
            EnsureComponent<NetworkObject>(prefabRoot);

            // Remove accidental nested NetworkObjects / NetworkTransforms anywhere under the prefab (children must not have them)
            CleanupNestedNetworking(prefabRoot);

            var player = FindChildByName(prefabRoot.transform, "Player");
            if (player == null) throw new Exception("Could not find child 'Player'. Your prefab hierarchy differs from expected.");

            // Owner-authoritative transform sync on ROOT (clients move their own player; root follows player via NetworkedCowsinsRootSync).
            var ownerNt = EnsureComponent<OwnerAuthoritativeNetworkTransform>(prefabRoot);
            ownerNt.Interpolate = true;
            ownerNt.SyncPositionX = true;
            ownerNt.SyncPositionY = true;
            ownerNt.SyncPositionZ = true;
            ownerNt.SyncRotAngleX = true;
            ownerNt.SyncRotAngleY = true;
            ownerNt.SyncRotAngleZ = true;
            ownerNt.SyncScaleX = false;
            ownerNt.SyncScaleY = false;
            ownerNt.SyncScaleZ = false;

            var rootSync = EnsureComponent<NetworkedCowsinsRootSync>(prefabRoot);
            var soSync = new SerializedObject(rootSync);
            soSync.FindProperty("playerTransform").objectReferenceValue = player;
            soSync.ApplyModifiedPropertiesWithoutUndo();

            var playerController = EnsureComponent<NetworkedCowsinsPlayerController>(prefabRoot);
            var pm = player.GetComponent<PlayerMovement>();
            if (pm == null) throw new Exception("Player object is missing cowsins.PlayerMovement.");

            // Wire PlayerMovement field
            var soPc = new SerializedObject(playerController);
            soPc.FindProperty("playerMovement").objectReferenceValue = pm;
            soPc.FindProperty("movingTransform").objectReferenceValue = player;

            // Owner-only objects: Camera + PlayerUI (if present)
            var camera = FindChildByName(prefabRoot.transform, "Camera");
            var ui = FindChildByName(prefabRoot.transform, "PlayerUI");
            var ownerOnlyProp = soPc.FindProperty("ownerOnlyObjects");
            ownerOnlyProp.arraySize = 0;
            if (camera != null) { ownerOnlyProp.InsertArrayElementAtIndex(ownerOnlyProp.arraySize); ownerOnlyProp.GetArrayElementAtIndex(ownerOnlyProp.arraySize - 1).objectReferenceValue = camera.gameObject; }
            if (ui != null) { ownerOnlyProp.InsertArrayElementAtIndex(ownerOnlyProp.arraySize); ownerOnlyProp.GetArrayElementAtIndex(ownerOnlyProp.arraySize - 1).objectReferenceValue = ui.gameObject; }

            // Local player hidden: PlayerGraphics (full body) - owner sees only arms, others see full body
            var playerGraphics = FindChildByName(prefabRoot.transform, "PlayerGraphics");
            var localHiddenProp = soPc.FindProperty("localPlayerHiddenObjects");
            localHiddenProp.arraySize = 0;
            if (playerGraphics != null)
            {
                localHiddenProp.InsertArrayElementAtIndex(localHiddenProp.arraySize);
                localHiddenProp.GetArrayElementAtIndex(localHiddenProp.arraySize - 1).objectReferenceValue = playerGraphics.gameObject;
            }

            soPc.ApplyModifiedPropertiesWithoutUndo();

            // InputManager object (separate child)
            var inputManagerGo = FindChildByName(prefabRoot.transform, "InputManager");
            if (inputManagerGo != null)
            {
                if (inputManagerGo.GetComponent<InputManager>() == null)
                {
                    Debug.LogWarning("[NetworkedCowsinsSetup] Found 'InputManager' object but it has no cowsins.InputManager component.");
                }

                var inputGate = EnsureComponent<NetworkedCowsinsInputManager>(inputManagerGo.gameObject);
                // Provide authority source: the player's NetworkBehaviour
                inputGate.SetOwnerAuthority(playerController);

                // Also wire reference into player controller
                var soPc2 = new SerializedObject(playerController);
                soPc2.FindProperty("networkedInputManager").objectReferenceValue = inputGate;
                soPc2.ApplyModifiedPropertiesWithoutUndo();
            }

            // State sync must live on ROOT (NetworkBehaviour requires a NetworkObject; nested NetworkObjects are not supported on spawned prefabs)
            var stateSync = EnsureComponent<NetworkedCowsinsStateSync>(prefabRoot);
            var soState = new SerializedObject(stateSync);
            soState.FindProperty("playerMovement").objectReferenceValue = pm;
            soState.ApplyModifiedPropertiesWithoutUndo();

            // Fixups for Cowsins scripts that NRE on OnEnable before Start
            EnsureComponent<CowsinsNetcodeFixups>(player.gameObject);
            EnsureComponent<CowsinsPreEnableGuard>(player.gameObject);

            // Networked health so Cowsins weapon hits apply damage over the network (IDamageable on root).
            EnsureComponent<NetworkedHealth>(prefabRoot);

            // Ensure PlayerDependencies serialized references are wired (prevents CameraEffects NRE).
            WirePlayerDependencies(player.gameObject, prefabRoot);

            // PvP: include Player layer in weapon hit layer so shots can hit other players
            IncludePlayerLayerInWeaponHitLayer(player.gameObject);
        }

        private static void IncludePlayerLayerInWeaponHitLayer(GameObject playerGo)
        {
            var wc = playerGo.GetComponent<WeaponController>();
            if (wc == null) return;
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer < 0) return;
            var so = new SerializedObject(wc);
            var hitLayerProp = so.FindProperty("settings.hitLayer.m_Bits");
            if (hitLayerProp == null) return;
            int bits = hitLayerProp.intValue;
            bits |= (1 << playerLayer);
            hitLayerProp.intValue = bits;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CleanupNestedNetworking(GameObject prefabRoot)
        {
            // This mirrors the runtime cleanup we had in PlayerSpawner, but we do it once in-editor so the prefab is correct.
            // Rules for spawned player prefab:
            // - Exactly one NetworkObject on the root
            // - No NetworkBehaviours and no NetworkObject on any child
            var rootTransform = prefabRoot.transform;
            var rootNo = prefabRoot.GetComponent<NetworkObject>();

            int removedNetworkObjects = 0;
            int removedNetworkBehaviours = 0;
            int removedNetworkTransforms = 0;

            var transforms = prefabRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                var t = transforms[i];
                if (t == null || t == rootTransform) continue;

                var go = t.gameObject;

                // Remove NetworkBehaviours from children (they would require a child NetworkObject -> not supported here).
                var nbs = go.GetComponents<NetworkBehaviour>();
                if (nbs != null && nbs.Length > 0)
                {
                    for (int j = 0; j < nbs.Length; j++)
                    {
                        var nb = nbs[j];
                        if (nb == null) continue;
                        removedNetworkBehaviours++;
                        Object.DestroyImmediate(nb, true);
                    }
                }

                // Remove any network transform flavours from children.
                if (go.GetComponent<NetworkTransform>() != null) { removedNetworkTransforms++; RemoveIfExists<NetworkTransform>(go); }
                if (go.GetComponent<OwnerAuthoritativeNetworkTransform>() != null) { removedNetworkTransforms++; RemoveIfExists<OwnerAuthoritativeNetworkTransform>(go); }

                // Remove child NetworkObject last (after NetworkBehaviours are gone).
                var childNo = go.GetComponent<NetworkObject>();
                if (childNo != null && childNo != rootNo)
                {
                    removedNetworkObjects++;
                    Object.DestroyImmediate(childNo, true);
                }
            }

            // Final sanity: if root NetworkObject somehow ended up missing, restore it.
            if (prefabRoot.GetComponent<NetworkObject>() == null)
            {
                EnsureComponent<NetworkObject>(prefabRoot);
            }

            // Lightweight summary in console for visibility during setup.
            Debug.Log($"[NetworkedCowsinsSetup] Cleanup nested networking: removed {removedNetworkObjects} child NetworkObject(s), {removedNetworkBehaviours} child NetworkBehaviour(s), {removedNetworkTransforms} child NetworkTransform(s).");
        }

        private static void WirePlayerDependencies(GameObject playerGo, GameObject prefabRoot)
        {
            var deps = playerGo.GetComponent<PlayerDependencies>();
            if (deps == null) return;

            var so = new SerializedObject(deps);

            // InputManager is on child "InputManager"
            var inputGo = FindChildByName(prefabRoot.transform, "InputManager");
            if (inputGo != null)
            {
                var input = inputGo.GetComponent<InputManager>();
                if (input != null) so.FindProperty("inputManager").objectReferenceValue = input;
            }

            // Components that are typically on Player
            var camFx = playerGo.GetComponent<CameraEffects>();
            if (camFx != null) so.FindProperty("cameraEffects").objectReferenceValue = camFx;

            var weaponFx = playerGo.GetComponent<WeaponEffects>();
            if (weaponFx != null) so.FindProperty("weaponEffects").objectReferenceValue = weaponFx;

            // CameraFOVManager usually on Camera child
            var cam = FindChildByName(prefabRoot.transform, "Camera");
            if (cam != null)
            {
                var fov = cam.GetComponent<CameraFOVManager>();
                if (fov != null) so.FindProperty("cameraFOVManager").objectReferenceValue = fov;
            }

            // UIController/Crosshair/UIEffects usually on PlayerUI child
            var ui = FindChildByName(prefabRoot.transform, "PlayerUI");
            if (ui != null)
            {
                var uiController = ui.GetComponent<UIController>();
                if (uiController != null) so.FindProperty("uiController").objectReferenceValue = uiController;

                var crosshair = ui.GetComponentInChildren<Crosshair>(true);
                if (crosshair != null) so.FindProperty("crosshair").objectReferenceValue = crosshair;

                var uiFx = ui.GetComponent<UIEffects>();
                if (uiFx != null) so.FindProperty("uIEffects").objectReferenceValue = uiFx;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        private static void RemoveIfExists<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) return;
            Object.DestroyImmediate(c, true);
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var found = FindChildByName(child, name);
                if (found != null) return found;
            }
            return null;
        }
        
        [MenuItem("Tools/Networked Cowsins/Setup Guide")]
        public static void OpenSetupGuide()
        {
            string guidePath = "Assets/Scripts/Core/NetworkedCowsins/NETWORKED_COWSINS_SETUP.md";
            Object guide = AssetDatabase.LoadAssetAtPath<Object>(guidePath);
            
            if (guide != null)
            {
                Selection.activeObject = guide;
                EditorGUIUtility.PingObject(guide);
            }
            else
            {
                Debug.LogWarning($"Setup guide not found at: {guidePath}");
                EditorUtility.DisplayDialog("Info", 
                    "Setup guide will be available after creating the NetworkedCowsins scripts.\n" +
                    "Please run 'Create Networked Player Prefab' first.", "OK");
            }
        }
        
        [MenuItem("Tools/Networked Cowsins/Verify Setup")]
        public static void VerifySetup()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("Networked Cowsins Setup Verification");
            report.AppendLine("=====================================");
            
            // Check if scripts exist
            bool playerControllerExists = Type.GetType("HeistNSeek.Core.NetworkedCowsins.NetworkedCowsinsPlayerController, Assembly-CSharp") != null;
            bool inputManagerExists = Type.GetType("HeistNSeek.Core.NetworkedCowsins.NetworkedCowsinsInputManager, Assembly-CSharp") != null;
            
            report.AppendLine($"1. Scripts Check:");
            report.AppendLine($"   - NetworkedCowsinsPlayerController: {(playerControllerExists ? "✓ Found" : "✗ Missing")}");
            report.AppendLine($"   - NetworkedCowsinsInputManager: {(inputManagerExists ? "✓ Found" : "✗ Missing")}");
            
            // Check if prefab exists
            string prefabPath = "Assets/Prefabs/Networked/NetworkedMovementCowsinsFPSController.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            report.AppendLine($"\n2. Prefab Check:");
            report.AppendLine($"   - Networked prefab: {(prefab != null ? $"✓ Found at {prefabPath}" : $"✗ Missing at {prefabPath}")}");
            
            if (prefab != null)
            {
                // Root network object + components
                NetworkObject rootNetworkObject = prefab.GetComponent<NetworkObject>();
                OwnerAuthoritativeNetworkTransform rootNetworkTransform = prefab.GetComponent<OwnerAuthoritativeNetworkTransform>();
                NetworkedCowsinsRootSync rootSync = prefab.GetComponent<NetworkedCowsinsRootSync>();
                NetworkedCowsinsPlayerController playerController = prefab.GetComponent<NetworkedCowsinsPlayerController>();

                // Player child (no NetworkObject expected)
                var player = FindChildByName(prefab.transform, "Player");
                NetworkObject playerNetworkObject = player != null ? player.GetComponent<NetworkObject>() : null;
                NetworkedCowsinsStateSync stateSync = player != null ? player.GetComponent<NetworkedCowsinsStateSync>() : null;
                CowsinsNetcodeFixups fixups = player != null ? player.GetComponent<CowsinsNetcodeFixups>() : null;

                // InputManager child gate
                var inputGo = FindChildByName(prefab.transform, "InputManager");
                NetworkedCowsinsInputManager networkedInputManager = inputGo != null ? inputGo.GetComponent<NetworkedCowsinsInputManager>() : null;
                InputManager originalInputManager = inputGo != null ? inputGo.GetComponent<InputManager>() : null;
                
                report.AppendLine($"\n3. Prefab Components:");
                report.AppendLine($"   - Root NetworkObject: {(rootNetworkObject != null ? "✓ Present" : "✗ Missing")}");
                report.AppendLine($"   - Root OwnerAuthoritativeNetworkTransform: {(rootNetworkTransform != null ? "✓ Present" : "✗ Missing")}");
                report.AppendLine($"   - Root NetworkedCowsinsRootSync: {(rootSync != null ? "✓ Present" : "✗ Missing")}");
                report.AppendLine($"   - Root NetworkedCowsinsPlayerController: {(playerController != null ? "✓ Present" : "✗ Missing")}");
                var networkedHealth = prefab.GetComponent<NetworkedHealth>();
                report.AppendLine($"   - Root NetworkedHealth (weapon damage): {(networkedHealth != null ? "✓ Present" : "○ Optional")}");
                report.AppendLine($"   - Player NetworkObject: {(playerNetworkObject != null ? "⚠ Present (should NOT be on child)" : "✓ Not present")}");
                report.AppendLine($"   - Player NetworkedCowsinsStateSync: {(stateSync != null ? "✓ Present" : "✗ Missing")}");
                report.AppendLine($"   - Player CowsinsNetcodeFixups: {(fixups != null ? "✓ Present" : "✗ Missing")}");
                report.AppendLine($"   - InputManager NetworkedCowsinsInputManager: {(networkedInputManager != null ? "✓ Present" : "✗ Missing")}");
                report.AppendLine($"   - InputManager cowsins.InputManager: {(originalInputManager != null ? "✓ Present" : "✗ Missing")}");
                
                if (playerController != null)
                {
                    SerializedObject so = new SerializedObject(playerController);
                    SerializedProperty playerMovementProp = so.FindProperty("playerMovement");
                    SerializedProperty inputManagerProp = so.FindProperty("networkedInputManager");
                    SerializedProperty movingTransformProp = so.FindProperty("movingTransform");
                    
                    report.AppendLine($"\n4. Component References:");
                    report.AppendLine($"   - PlayerMovement reference: {(playerMovementProp.objectReferenceValue != null ? "✓ Set" : "✗ Not set")}");
                    report.AppendLine($"   - NetworkedInputManager reference: {(inputManagerProp.objectReferenceValue != null ? "✓ Set" : "✗ Not set")}");
                    report.AppendLine($"   - MovingTransform reference: {(movingTransformProp.objectReferenceValue != null ? "✓ Set" : "✗ Not set")}");
                }
            }
            
            // Display report
            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("Setup Verification", report.ToString(), "OK");
        }
    }
#endif
}