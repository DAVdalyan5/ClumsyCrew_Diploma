using System;
using Assets.Scripts.Core.Inventory.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeistNSeek.UI.PeekInventory
{
    /// <summary>
    /// Single slot in the peek inventory grid. Displays item icon and amount.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PeekInventorySlotUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Sprite emptySlotSprite;

        private string _itemId;
        private int _amount;

        public string ItemId => _itemId;
        public int Amount => _amount;

        public event Action<PeekInventorySlotUI> OnSlotClicked;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        public void SetItem(string itemId, ItemDataSO itemData, int amount)
        {
            _itemId = itemId;
            _amount = amount;

            if (iconImage != null)
            {
                iconImage.sprite = itemData != null && itemData.icon != null ? itemData.icon : emptySlotSprite;
                iconImage.enabled = true;
            }

            if (amountText != null)
            {
                string name = itemData != null ? itemData.itemName : (itemId ?? "?");
                amountText.text = $"{name}: {amount}";
                amountText.gameObject.SetActive(true);
            }
        }

        public void SetEmpty()
        {
            _itemId = null;
            _amount = 0;

            if (iconImage != null)
            {
                iconImage.sprite = emptySlotSprite;
                iconImage.enabled = emptySlotSprite != null;
            }

            if (amountText != null)
            {
                amountText.text = string.Empty;
                amountText.gameObject.SetActive(false);
            }
        }

        private void HandleClicked()
        {
            if (!string.IsNullOrEmpty(_itemId) && _amount > 0)
            {
                OnSlotClicked?.Invoke(this);
            }
        }
    }
}
