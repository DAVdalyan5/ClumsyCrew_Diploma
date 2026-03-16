#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.AI;
using HeistNSeek.Core.Enemy;

namespace HeistNSeek.Core.Enemy.Editor
{
    public static class EnemyPrefabSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Enemy/PatrolEnemy.prefab";
        private const string RioRagdollPath = "Assets/Prefabs/HalfBaked/RioRagdoll.prefab";

        [MenuItem("Tools/Enemy/Create Patrol Enemy Prefab")]
        public static void CreatePatrolEnemyPrefab()
        {
            EnsureEnemyFolderExists();

            GameObject root = new GameObject("PatrolEnemy");
            root.tag = "Enemy";

            try
            {
                AddNetworkComponents(root);
                AddNavMeshAgent(root);
                AddPatrolEnemyController(root);
                AddRioRagdoll(root);
                AddAimCamera(root);
                AddEnemyBody(root);
                AddNetworkedEnemyHealth(root);
                AddEnemyWeaponController(root);
                AddEnemyWeaponShooter(root);
                WireWeaponShooterToController(root);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab != null)
                {
                    Debug.Log($"[EnemyPrefabSetup] Created prefab at {PrefabPath}");
                    EditorUtility.DisplayDialog("Success", $"Patrol enemy prefab created at:\n{PrefabPath}\n\nNext steps:\n1. Add to NetworkManager's Network Prefabs list\n2. Assign Weapon_SO (e.g. Pistol, Rifle) to EnemyWeaponController\n3. Set hitLayer to include Player layer\n4. Assign patrol waypoints in scene\n5. Bake NavMesh", "OK");
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
                else
                {
                    Debug.LogError("[EnemyPrefabSetup] Failed to save prefab");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void EnsureEnemyFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemy"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Enemy");
        }

        private static void AddNetworkComponents(GameObject root)
        {
            if (root.GetComponent<NetworkObject>() == null)
                root.AddComponent<NetworkObject>();

            var nt = root.GetComponent<NetworkTransform>();
            if (nt == null)
                nt = root.AddComponent<NetworkTransform>();
            nt.Interpolate = true;
            nt.SyncPositionX = true;
            nt.SyncPositionY = true;
            nt.SyncPositionZ = true;
            nt.SyncRotAngleX = true;
            nt.SyncRotAngleY = true;
            nt.SyncRotAngleZ = true;
        }

        private static void AddNavMeshAgent(GameObject root)
        {
            var agent = root.GetComponent<NavMeshAgent>();
            if (agent == null)
                agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 3.5f;
            agent.angularSpeed = 120f;
            agent.stoppingDistance = 0.5f;
        }

        private static void AddPatrolEnemyController(GameObject root)
        {
            var ctrl = root.GetComponent<PatrolEnemyController>();
            if (ctrl == null)
                ctrl = root.AddComponent<PatrolEnemyController>();
        }

        private static void WireWeaponShooterToController(GameObject root)
        {
            var patrol = root.GetComponent<PatrolEnemyController>();
            var shooter = root.GetComponent<EnemyWeaponShooter>();
            if (patrol != null && shooter != null)
            {
                var so = new SerializedObject(patrol);
                so.FindProperty("weaponShooter").objectReferenceValue = shooter;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AddNetworkedEnemyHealth(GameObject root)
        {
            var health = root.GetComponent<NetworkedEnemyHealth>();
            if (health == null)
                health = root.AddComponent<NetworkedEnemyHealth>();

            var rio = FindChildByName(root.transform, "RioRagdoll");
            if (rio != null)
            {
                var so = new SerializedObject(health);
                so.FindProperty("ragdollHierarchyPart").objectReferenceValue = rio.gameObject;
                var hips = FindChildByName(rio, "Hips") ?? FindChildByName(rio, "Pelvis") ?? FindChildByName(rio, "Spine");
                if (hips != null)
                    so.FindProperty("ragdollPositionRoot").objectReferenceValue = hips;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AddEnemyWeaponController(GameObject root)
        {
            var ctrl = root.GetComponent<EnemyWeaponController>();
            if (ctrl == null)
                ctrl = root.AddComponent<EnemyWeaponController>();

            var aimCam = FindChildByName(root.transform, "AimCamera");
            if (aimCam != null)
            {
                var so = new SerializedObject(ctrl);
                so.FindProperty("aimCamera").objectReferenceValue = aimCam.GetComponent<Camera>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AddEnemyWeaponShooter(GameObject root)
        {
            var shooter = root.GetComponent<EnemyWeaponShooter>();
            if (shooter == null)
                shooter = root.AddComponent<EnemyWeaponShooter>();

            var so = new SerializedObject(shooter);
            so.FindProperty("weaponController").objectReferenceValue = root.GetComponent<EnemyWeaponController>();
            so.FindProperty("enemyController").objectReferenceValue = root.GetComponent<PatrolEnemyController>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddRioRagdoll(GameObject root)
        {
            if (FindChildByName(root.transform, "RioRagdoll") != null)
                return;

            var rioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RioRagdollPath);
            if (rioPrefab == null)
            {
                Debug.LogWarning($"[EnemyPrefabSetup] RioRagdoll not found at {RioRagdollPath}. Add manually.");
                return;
            }

            var rio = (GameObject)PrefabUtility.InstantiatePrefab(rioPrefab);
            rio.name = "RioRagdoll";
            rio.transform.SetParent(root.transform, false);
            rio.transform.localPosition = Vector3.zero;
            rio.transform.localRotation = Quaternion.identity;
            rio.transform.localScale = Vector3.one;
        }

        private static void AddAimCamera(GameObject root)
        {
            if (FindChildByName(root.transform, "AimCamera") != null)
                return;

            var go = new GameObject("AimCamera");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(0f, 1.5f, 0.5f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.Nothing;
            cam.cullingMask = 0;
        }

        private static void AddEnemyBody(GameObject root)
        {
            if (FindChildByName(root.transform, "EnemyBody") != null)
                return;

            var go = new GameObject("EnemyBody");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var col = go.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.4f;
            col.center = new Vector3(0f, 1f, 0f);

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
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
    }
}
#endif
