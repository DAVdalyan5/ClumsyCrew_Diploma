#if UNITY_EDITOR
using HeistNSeek.UI.PeekInventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HeistNSeek.Editor
{
    /// <summary>
    /// Creates PeekInventorySlotUI and PeekInventoryPopup prefabs at design time.
    /// Run: HeistNSeek > Setup Peek Inventory (creates prefabs + adds popup to Canvas)
    /// </summary>
    public static class PeekInventoryPrefabCreator
    {
        private const string PrefabsPath = "Assets/Prefabs/UI";

        [MenuItem("HeistNSeek/Develper Tools/Setup Peek Inventory (Create Prefabs + Add to Scene)")]
        public static void SetupPeekInventory()
        {
            CreatePrefabs();
            AddPopupToScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PeekInventoryPrefabCreator] Peek Inventory setup complete. Save the scene.");
        }

        [MenuItem("HeistNSeek/Develper Tools/Create Peek Inventory Prefabs")]
        public static void CreatePrefabs()
        {
            EnsureDirectoryExists();
            CreateSlotPrefab();
            CreatePopupPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PeekInventoryPrefabCreator] Prefabs created at " + PrefabsPath);
        }

        [MenuItem("HeistNSeek/Develper Tools/Add Peek Inventory Popup to Scene")]
        public static void AddPopupToScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/PeekInventoryPopup.prefab");
            if (prefab == null)
            {
                Debug.LogError("[PeekInventoryPrefabCreator] PeekInventoryPopup prefab not found. Run 'Create Peek Inventory Prefabs' first.");
                return;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[PeekInventoryPrefabCreator] No Canvas found in scene.");
                return;
            }

            var existing = canvas.GetComponentInChildren<PeekInventoryPopup>(true);
            if (existing != null)
            {
                Debug.Log("[PeekInventoryPrefabCreator] PeekInventoryPopup already exists in scene.");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(canvas.transform, false);
            instance.name = "PeekInventoryPopup";
            instance.SetActive(true);

            var rect = instance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Add Peek Inventory Popup");
            Selection.activeGameObject = instance;
            Debug.Log("[PeekInventoryPrefabCreator] PeekInventoryPopup added to Canvas.");
        }

        private static void EnsureDirectoryExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }
        }

        private static void CreateSlotPrefab()
        {
            var slotRoot = new GameObject("PeekInventorySlotUI");
            var rect = slotRoot.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64, 64);

            var bgImage = slotRoot.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            bgImage.raycastTarget = true;
            slotRoot.AddComponent<Button>();

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotRoot.transform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4, 4);
            iconRect.offsetMax = new Vector2(-4, -24);
            var iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;

            var amountObj = new GameObject("Amount");
            amountObj.transform.SetParent(slotRoot.transform, false);
            var amountRect = amountObj.AddComponent<RectTransform>();
            amountRect.anchorMin = new Vector2(0, 0);
            amountRect.anchorMax = new Vector2(1, 0);
            amountRect.pivot = new Vector2(0.5f, 0);
            amountRect.anchoredPosition = new Vector2(0, 2);
            amountRect.sizeDelta = new Vector2(-8, 18);
            var amountText = amountObj.AddComponent<TextMeshProUGUI>();
            amountText.text = "";
            amountText.fontSize = 12;
            amountText.alignment = TextAlignmentOptions.BottomRight;
            amountText.raycastTarget = false;

            var slotUI = slotRoot.AddComponent<PeekInventorySlotUI>();
            var so = new SerializedObject(slotUI);
            so.FindProperty("iconImage").objectReferenceValue = iconImage;
            so.FindProperty("amountText").objectReferenceValue = amountText;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(slotRoot, $"{PrefabsPath}/PeekInventorySlotUI.prefab");
            Object.DestroyImmediate(slotRoot);
        }

        private static void CreatePopupPrefab()
        {
            var popupRoot = new GameObject("PeekInventoryPopup");
            var popupRect = popupRoot.AddComponent<RectTransform>();
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;

            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(popupRoot.transform, false);
            var overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.01f);
            overlayImage.raycastTarget = true;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(popupRoot.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(400, 300);
            panelRect.anchoredPosition = Vector2.zero;
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.98f);
            panelImage.raycastTarget = true;

            var slotsContainer = new GameObject("SlotsContainer");
            slotsContainer.transform.SetParent(panel.transform, false);
            var slotsRect = slotsContainer.AddComponent<RectTransform>();
            slotsRect.anchorMin = Vector2.zero;
            slotsRect.anchorMax = Vector2.one;
            slotsRect.offsetMin = new Vector2(16, 16);
            slotsRect.offsetMax = new Vector2(-16, -16);
            var gridLayout = slotsContainer.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(64, 64);
            gridLayout.spacing = new Vector2(8, 8);
            gridLayout.padding = new RectOffset(16, 16, 16, 16);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;

            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/PeekInventorySlotUI.prefab");
            if (slotPrefab == null)
            {
                Debug.LogWarning("[PeekInventoryPrefabCreator] PeekInventorySlotUI prefab not found. Create slot prefab first.");
            }

            var popupComponent = popupRoot.AddComponent<PeekInventoryPopup>();
            var so = new SerializedObject(popupComponent);
            so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab != null ? slotPrefab.GetComponent<PeekInventorySlotUI>() : null;
            so.FindProperty("slotsContainer").objectReferenceValue = slotsContainer.transform;
            so.FindProperty("overlayBackground").objectReferenceValue = overlay;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(popupRoot, $"{PrefabsPath}/PeekInventoryPopup.prefab");
            Object.DestroyImmediate(popupRoot);
        }
    }
}
#endif
