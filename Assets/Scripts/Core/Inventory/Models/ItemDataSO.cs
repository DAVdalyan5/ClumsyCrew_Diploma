using UnityEngine;

namespace Assets.Scripts.Core.Inventory.Models
{
    [CreateAssetMenu(fileName = "ItemDataSO", menuName = "Robbery/Item")]
    public class ItemDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string itemName;
        public string description;
        public Sprite icon; // For inventory UI
        public GameObject prefab; // 3D model

        [Header("Properties")]
        public ItemType itemType; // Loot, Tool, Weapon, etc.
        public float weight; // Affects carrying capacity
        public int price; // Sell price or heist value
        public Rarity rarity;

        [Header("Gameplay")]
        public bool isStackable;
        public int maxStackSize = 1;
        public bool canBeSold;
    }

}
