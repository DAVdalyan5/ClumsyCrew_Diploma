using Assets.Scripts.Core.Inventory.Models;
using UnityEngine;

namespace HeistNSeek.Core.Inventory.NetworkedInventory
{
    /// <summary>
    /// Initializes the ItemRegistry with all available items at scene start.
    /// Add this to the Bootstrap or Main scene to ensure items are registered before use.
    /// </summary>
    public class ItemRegistryInitializer : MonoBehaviour
    {
        [Header("Item Registration")]
        [Tooltip("All item ScriptableObjects to register. These will be available for network spawning.")]
        [SerializeField] private ItemDataSO[] items;

        [Header("Settings")]
        [Tooltip("If true, also load items from Resources/Items folder")]
        [SerializeField] private bool loadFromResources = false;
        [SerializeField] private string resourcesPath = "Items";

        [Tooltip("If true, persist this object across scenes")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        private static bool _isInitialized = false;

        private void Awake()
        {
            // Ensure only one initializer runs
            if (_isInitialized)
            {
                Debug.Log("[ItemRegistryInitializer] Already initialized. Skipping.");
                Destroy(gameObject);
                return;
            }

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            InitializeRegistry();
            _isInitialized = true;
        }

        private void InitializeRegistry()
        {
            // Clear any existing registrations
            ItemRegistry.Clear();

            // Register items from the serialized array
            if (items != null && items.Length > 0)
            {
                ItemRegistry.Initialize(items);
                Debug.Log($"[ItemRegistryInitializer] Registered {items.Length} items from serialized array.");
            }

            // Optionally load from resources
            if (loadFromResources)
            {
                var resourceItems = Resources.LoadAll<ItemDataSO>(resourcesPath);
                if (resourceItems != null && resourceItems.Length > 0)
                {
                    foreach (var item in resourceItems)
                    {
                        ItemRegistry.RegisterItem(item);
                    }
                    Debug.Log($"[ItemRegistryInitializer] Registered {resourceItems.Length} items from Resources/{resourcesPath}.");
                }
            }
        }

        private void OnDestroy()
        {
            if (dontDestroyOnLoad)
            {
                // Reset static flag when destroyed
                _isInitialized = false;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Find All Items in Project")]
        private void FindAllItemsInProject()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:ItemDataSO");
            var foundItems = new ItemDataSO[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                foundItems[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDataSO>(path);
            }

            items = foundItems;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[ItemRegistryInitializer] Found {foundItems.Length} items in project.");
        }
#endif
    }
}
