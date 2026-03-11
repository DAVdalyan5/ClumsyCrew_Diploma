#if UNITY_EDITOR
using Assets.Scripts.Core.Inventory;
using Assets.Scripts.Core.Inventory.Models;
using UnityEditor;
using UnityEngine;

namespace HeistNSeek.Editor
{
    /// <summary>
    /// Editor window to create new item prefabs based on ItemBase.
    /// Menu: HeistNSeek > Create Item
    /// Creates an ItemDataSO and a world prefab wired up and ready to use.
    /// </summary>
    public class ItemCreatorWindow : EditorWindow
    {
        // ── Paths ────────────────────────────────────────────────────────────
        private const string ItemBasePrefabPath = "Assets/Prefabs/Items/Base/ItemBase.prefab";
        private const string DefaultPrefabFolder = "Assets/Prefabs/Items";
        private const string DefaultSOFolder     = "Assets/SO/Items";

        // ── Save paths ───────────────────────────────────────────────────────
        private string _prefabSaveFolder = DefaultPrefabFolder;
        private string _soSaveFolder     = DefaultSOFolder;

        // ── Prefab name ──────────────────────────────────────────────────────
        private string _prefabName = "NewItem";

        // ── ItemDataSO mode ──────────────────────────────────────────────────
        private bool _createNewSO = true;

        // New SO fields
        private string _itemName = "New Item";
        private string _description = "";
        private Sprite _icon = null;
        private ItemType _itemType = ItemType.Loot;
        private float _weight = 1f;
        private int _price = 0;
        private Rarity _rarity = Rarity.Common;
        private bool _isStackable = false;
        private int _maxStackSize = 1;
        private bool _canBeSold = true;

        // Existing SO
        private ItemDataSO _existingSO = null;

        // ── Visual ───────────────────────────────────────────────────────────
        private GameObject _modelPrefab = null;
        private Vector3    _meshScale   = Vector3.one;

        // ── NetworkedItem settings ───────────────────────────────────────────
        private string _playerTag = "Player";
        private bool _autoPickup = true;
        private float _pickupCooldown = 0.5f;

        // ── UI state ─────────────────────────────────────────────────────────
        private Vector2 _scroll;
        private string _statusMessage = "";
        private bool _statusIsError = false;

        // ────────────────────────────────────────────────────────────────────
        [MenuItem("HeistNSeek/Create Item")]
        public static void Open()
        {
            var window = GetWindow<ItemCreatorWindow>("Item Creator");
            window.minSize = new Vector2(380, 560);
        }

        // ────────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader("Item Creator", "Creates a prefab based on ItemBase with all fields wired up.");
            EditorGUILayout.Space(4);

            // ── Prefab Name ─────────────────────────────────────────────────
            DrawSection("Prefab");
            _prefabName = EditorGUILayout.TextField("Prefab Name", _prefabName);
            DrawFolderField("Save Folder", ref _prefabSaveFolder, DefaultPrefabFolder, "Select Prefab Save Folder");

            // ── Item Data ────────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            DrawSection("Item Data (SO)");

            _createNewSO = GUILayout.Toolbar(_createNewSO ? 0 : 1,
                new[] { "Create New", "Use Existing" }) == 0;

            EditorGUILayout.Space(2);

            if (_createNewSO)
                DrawNewSOFields();
            else
                DrawExistingSOFields();

            // ── Visual Model ─────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            DrawSection("Visual (optional)");
            _modelPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Model", _modelPrefab, typeof(GameObject), false);
            if (_modelPrefab != null)
                _meshScale = EditorGUILayout.Vector3Field("Scale", _meshScale);

            // ── NetworkedItem Settings ───────────────────────────────────────
            EditorGUILayout.Space(4);
            DrawSection("Pickup Settings");
            _playerTag = EditorGUILayout.TextField("Player Tag", _playerTag);
            _autoPickup = EditorGUILayout.Toggle("Auto Pickup", _autoPickup);
            _pickupCooldown = EditorGUILayout.FloatField("Pickup Cooldown", _pickupCooldown);

            // ── Create Button ────────────────────────────────────────────────
            EditorGUILayout.Space(8);
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Create Item Prefab", GUILayout.Height(34)))
                TryCreate();
            GUI.backgroundColor = Color.white;

            // ── Status ───────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.Space(4);
                var style = new GUIStyle(EditorStyles.helpBox);
                GUI.color = _statusIsError ? new Color(1f, 0.5f, 0.5f) : new Color(0.6f, 1f, 0.6f);
                EditorGUILayout.LabelField(_statusMessage, style);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndScrollView();
        }

        // ── Draw helpers ─────────────────────────────────────────────────────

        private static void DrawHeader(string title, string subtitle)
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.LabelField(title, titleStyle);
            var subStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            EditorGUILayout.LabelField(subtitle, subStyle);
        }

        private static void DrawSection(string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
            EditorGUILayout.Space(2);
        }

        private void DrawNewSOFields()
        {
            DrawFolderField("SO Save Folder", ref _soSaveFolder, DefaultSOFolder, "Select SO Save Folder");
            EditorGUILayout.Space(2);
            _itemName = EditorGUILayout.TextField("Item Name", _itemName);
            _description = EditorGUILayout.TextField("Description", _description);
            _icon = (Sprite)EditorGUILayout.ObjectField("Icon", _icon, typeof(Sprite), false);
            _itemType = (ItemType)EditorGUILayout.EnumPopup("Item Type", _itemType);
            _rarity = (Rarity)EditorGUILayout.EnumPopup("Rarity", _rarity);
            _weight = EditorGUILayout.FloatField("Weight", _weight);
            _price = EditorGUILayout.IntField("Price", _price);
            _isStackable = EditorGUILayout.Toggle("Is Stackable", _isStackable);
            if (_isStackable)
                _maxStackSize = EditorGUILayout.IntField("Max Stack Size", _maxStackSize);
            _canBeSold = EditorGUILayout.Toggle("Can Be Sold", _canBeSold);
        }

        private void DrawExistingSOFields()
        {
            _existingSO = (ItemDataSO)EditorGUILayout.ObjectField(
                "ItemDataSO", _existingSO, typeof(ItemDataSO), false);
        }

        // ── Creation logic ────────────────────────────────────────────────────

        private void TryCreate()
        {
            _statusMessage = "";
            _statusIsError = false;

            // Validate
            if (string.IsNullOrWhiteSpace(_prefabName))
            {
                SetError("Prefab Name cannot be empty.");
                return;
            }

            if (!_createNewSO && _existingSO == null)
            {
                SetError("Please assign an existing ItemDataSO.");
                return;
            }

            if (_createNewSO && string.IsNullOrWhiteSpace(_itemName))
            {
                SetError("Item Name cannot be empty.");
                return;
            }

            var itemBasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemBasePrefabPath);
            if (itemBasePrefab == null)
            {
                SetError($"ItemBase prefab not found at: {ItemBasePrefabPath}");
                return;
            }

            // Ensure folders exist
            EnsureFolderPath(_prefabSaveFolder);
            if (_createNewSO)
                EnsureFolderPath(_soSaveFolder);

            // Get or create ItemDataSO
            ItemDataSO so = _createNewSO ? CreateItemDataSO() : _existingSO;
            if (so == null)
            {
                SetError("Failed to create or find ItemDataSO.");
                return;
            }

            // Build prefab
            string prefabPath = $"{_prefabSaveFolder}/{_prefabName}.prefab";

            // Check for existing prefab
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                if (!EditorUtility.DisplayDialog("Prefab Exists",
                    $"'{_prefabName}.prefab' already exists. Overwrite?", "Overwrite", "Cancel"))
                    return;
            }

            // Instantiate ItemBase in memory
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(itemBasePrefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = _prefabName;

            // Wire up NetworkedItem fields via SerializedObject
            var networkedItem = instance.GetComponent("NetworkedItem") as MonoBehaviour;
            if (networkedItem != null)
            {
                var so2 = new SerializedObject(networkedItem);
                so2.FindProperty("preplacedItemData").objectReferenceValue = so;
                so2.FindProperty("playerTag").stringValue = _playerTag;
                so2.FindProperty("autoPickup").boolValue = _autoPickup;
                so2.FindProperty("pickupCooldown").floatValue = _pickupCooldown;

                // Find VisualContainer child
                var visualContainer = instance.transform.Find("VisualContainer");

                if (visualContainer != null)
                {
                    so2.FindProperty("visualRoot").objectReferenceValue = visualContainer.gameObject;

                    // Instantiate the full model under VisualContainer
                    if (_modelPrefab != null)
                    {
                        var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(_modelPrefab);
                        modelInstance.transform.SetParent(visualContainer, false);
                        modelInstance.transform.localPosition = Vector3.zero;
                        modelInstance.transform.localRotation = Quaternion.identity;
                        modelInstance.transform.localScale    = _meshScale;

                        // Wire the first MeshFilter/MeshRenderer found into NetworkedItem
                        var mf = modelInstance.GetComponentInChildren<MeshFilter>(true);
                        var mr = modelInstance.GetComponentInChildren<MeshRenderer>(true);
                        if (mf != null) so2.FindProperty("meshFilter").objectReferenceValue  = mf;
                        if (mr != null) so2.FindProperty("meshRenderer").objectReferenceValue = mr;
                    }
                }

                so2.ApplyModifiedPropertiesWithoutUndo();
            }

            // Save as prefab asset
            bool success;
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out success);
            DestroyImmediate(instance);

            if (!success)
            {
                SetError($"Failed to save prefab at: {prefabPath}");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Ping result in Project window
            var created = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            EditorGUIUtility.PingObject(created);
            Selection.activeObject = created;

            _statusMessage = $"'{_prefabName}.prefab' created successfully!";
            Debug.Log($"[ItemCreator] {_statusMessage}");
        }

        private ItemDataSO CreateItemDataSO()
        {
            var so = CreateInstance<ItemDataSO>();
            so.itemName = _itemName;
            so.description = _description;
            so.icon = _icon;
            so.itemType = _itemType;
            so.weight = _weight;
            so.price = _price;
            so.rarity = _rarity;
            so.isStackable = _isStackable;
            so.maxStackSize = _isStackable ? _maxStackSize : 1;
            so.canBeSold = _canBeSold;

            string soPath = $"{_soSaveFolder}/{_prefabName}.asset";

            // If SO already exists, ask
            if (AssetDatabase.LoadAssetAtPath<ItemDataSO>(soPath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog("SO Exists",
                    $"'{_prefabName}.asset' already exists in SO/Items. Overwrite?", "Overwrite", "Cancel");
                if (!overwrite)
                    return null;
                AssetDatabase.DeleteAsset(soPath);
            }

            AssetDatabase.CreateAsset(so, soPath);
            return so;
        }

        /// <summary>Creates every segment of an Assets-relative path if missing.</summary>
        private static void EnsureFolderPath(string path)
        {
            // path must start with "Assets"
            var parts = path.TrimEnd('/').Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        /// <summary>
        /// Draws a read-only path label + Browse button.
        /// Converts the OS absolute path returned by OpenFolderPanel back to Assets-relative.
        /// </summary>
        private static void DrawFolderField(string label, ref string folder, string defaultPath, string panelTitle)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);

            // Editable text field
            folder = EditorGUILayout.TextField(folder, GUILayout.ExpandWidth(true));

            // Browse button
            if (GUILayout.Button("...", GUILayout.Width(26)))
            {
                string absProject = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(Application.dataPath, ".."));

                // Start panel inside the current folder if it exists
                string startAbs = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(absProject, folder.Replace('/', System.IO.Path.DirectorySeparatorChar)));

                if (!System.IO.Directory.Exists(startAbs))
                    startAbs = Application.dataPath;

                string chosen = EditorUtility.OpenFolderPanel(panelTitle, startAbs, "");
                if (!string.IsNullOrEmpty(chosen))
                {
                    // Convert absolute → Assets-relative
                    string rel = chosen.Replace('\\', '/');
                    string proj = absProject.Replace('\\', '/').TrimEnd('/') + "/";
                    if (rel.StartsWith(proj))
                        folder = rel.Substring(proj.Length).TrimEnd('/');
                    else
                        EditorUtility.DisplayDialog("Invalid Folder",
                            "Please choose a folder inside this Unity project.", "OK");
                }
            }

            // Reset button
            if (GUILayout.Button("↺", GUILayout.Width(22)))
                folder = defaultPath;

            EditorGUILayout.EndHorizontal();
        }

        private void SetError(string msg)
        {
            _statusMessage = msg;
            _statusIsError = true;
            Debug.LogError($"[ItemCreator] {msg}");
        }
    }
}
#endif
