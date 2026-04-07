#if UNITY_EDITOR
using HeistNSeek.Core.NetworkedCowsins;
using UnityEditor;
using UnityEngine;

namespace HeistNSeek.Editor
{
    /// <summary>
    /// Builds <see cref="CowsinsSessionServices"/> prefab by cloning only the <b>GeneralManagers</b> subtree from the Cowsins player prefab
    /// (Pool, economy mirrors, Addon, ambient child, etc.). Player body / camera bones are not included—do not merge those into the output YAML.
    /// Run after pulling the repo: Tools / HeistNSeek / Build Cowsins Session Services Prefab.
    /// </summary>
    public static class CowsinsSessionServicesPrefabBuilder
    {
        private const string SourcePrefabPath = "Assets/ThirdParty/Cowsins/Prefabs/PlayerControllers/CowsinsFPSController.prefab";
        private const string OutputPrefabPath = "Assets/Prefabs/Networked/CowsinsSessionServices.prefab";

        [MenuItem("Tools/HeistNSeek/Build Cowsins Session Services Prefab")]
        public static void BuildPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            if (source == null)
            {
                Debug.LogError($"[CowsinsSessionServicesPrefabBuilder] Missing source prefab: {SourcePrefabPath}");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source));
            try
            {
                Transform generalManagers = FindDeepChild(root.transform, "GeneralManagers");
                if (generalManagers == null)
                {
                    Debug.LogError("[CowsinsSessionServicesPrefabBuilder] Could not find GeneralManagers in source prefab.");
                    return;
                }

                var instance = Object.Instantiate(generalManagers.gameObject);
                instance.name = "CowsinsSessionServices";
                instance.transform.SetParent(null, false);
                instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.transform.localScale = Vector3.one;

                if (instance.GetComponent<CowsinsSingletonLifetimeGuard>() == null)
                    instance.AddComponent<CowsinsSingletonLifetimeGuard>();

                EnsureFolderExists("Assets/Prefabs/Networked");
                PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath);
                Object.DestroyImmediate(instance);

                AssetDatabase.Refresh();
                Debug.Log($"[CowsinsSessionServicesPrefabBuilder] Saved {OutputPrefabPath}");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(OutputPrefabPath);
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("Tools/HeistNSeek/Verify Networked Cowsins Prefabs")]
        public static void VerifyPrefabs()
        {
            const string playerPrefabPath = "Assets/Prefabs/Player/NetworkedMovementCowsinsFPSController.prefab";
            const string sessionPath = OutputPrefabPath;

            var report = "";
            var session = AssetDatabase.LoadAssetAtPath<GameObject>(sessionPath);
            report += session != null
                ? $"Session prefab: OK ({sessionPath})\n"
                : $"Session prefab: MISSING ({sessionPath}) — run Build Cowsins Session Services Prefab.\n";

            if (session != null)
            {
                var sessionContents = PrefabUtility.LoadPrefabContents(sessionPath);
                try
                {
                    if (FindDeepChild(sessionContents.transform, "Head ( Camera Placement )") != null)
                        report += "Session prefab: ERROR — stray 'Head ( Camera Placement )' (broken parent refs). Re-run Build Cowsins Session Services Prefab.\n";
                    else if (sessionContents.transform.childCount == 0)
                        report += "Session prefab: WARNING — root has no children (expected PoolManager / AmbientSounds).\n";
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(sessionContents);
                }
            }

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
            if (playerPrefab == null)
            {
                report += $"Player prefab: MISSING ({playerPrefabPath})\n";
                EditorUtility.DisplayDialog("Verify Networked Cowsins", report, "OK");
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(playerPrefabPath);
            try
            {
                bool hasGm = FindDeepChild(contents.transform, "GeneralManagers") != null;
                report += hasGm
                    ? "Player prefab: WARNING — GeneralManagers still present (should be stripped for session services).\n"
                    : "Player prefab: OK — no GeneralManagers.\n";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            Debug.Log("[Verify Networked Cowsins]\n" + report);
            EditorUtility.DisplayDialog("Verify Networked Cowsins", report, "OK");
        }

        private static Transform FindDeepChild(Transform t, string name)
        {
            if (t.name == name)
                return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var found = FindDeepChild(t.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void EnsureFolderExists(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
                return;
            var parts = assetFolder.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
