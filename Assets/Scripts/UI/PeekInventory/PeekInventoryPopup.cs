using System.Collections.Generic;
using Assets.Scripts.Core.Inventory.Models;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using cowsins;
using Easy.MessageHub;
using HeistNSeek.Core.Inventory.NetworkedInventory;
using HeistNSeek.Events;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace HeistNSeek.UI.PeekInventory
{
    /// <summary>
    /// Popup that displays another player's inventory when peeking. Allows stealing items.
    /// Blocks control and frees mouse while open; closes on click outside.
    /// </summary>
    public class PeekInventoryPopup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PeekInventorySlotUI slotPrefab;
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private GameObject overlayBackground;

        private IMessageHub _messageHub;
        private NetworkedPlayerInventory _targetInventory;
        private readonly List<PeekInventorySlotUI> _slotInstances = new();

        private IPlayerControlProvider _playerControl;
        private UIController _uiController;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        private void Awake()
        {
            EnsureMessageHubResolved();
            if (_messageHub != null)
            {
                _messageHub.SubscribeSafe<PeekEvent>(this, OnPeekEvent);
            }

            HideWindow();

            var clickHandler = overlayBackground.GetComponent<PeekOverlayClickHandler>();
            if (clickHandler == null)
                clickHandler = overlayBackground.AddComponent<PeekOverlayClickHandler>();
            clickHandler.Init(Close);
        }

        private void EnsureMessageHubResolved()
        {
            if (_messageHub != null) return;

            LifetimeScope scope = FindAnyObjectByType<BootstrapLifetimeScope>();
            if (scope == null)
                scope = FindAnyObjectByType<LifetimeScope>();
            if (scope != null && scope.Container != null)
            {
                _messageHub = scope.Container.Resolve<IMessageHub>();
            }
        }

        private void OnPeekEvent(PeekEvent evt)
        {
            if (!NetworkManager.Singleton?.IsListening ?? true) return;

            ulong localId = NetworkManager.Singleton.LocalClientId;
            if (evt.TargetInventory == null || evt.TargetInventory.OwnerClientId == localId)
                return;

            ResolveDependencies();
            if (_playerControl == null || _uiController == null)
            {
                Debug.LogWarning("[PeekInventoryPopup] Could not resolve PlayerControl or UIController.");
                return;
            }

            if (_targetInventory != null)
            {
                _targetInventory.OnInventoryChanged -= RefreshGrid;
            }

            _targetInventory = evt.TargetInventory;
            _targetInventory.OnInventoryChanged += RefreshGrid;

            ShowPopup();
        }

        private void ResolveDependencies()
        {
            if (_playerControl != null && _uiController != null) return;

            var localPlayer = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localPlayer != null)
            {
                var deps = localPlayer.GetComponentInChildren<PlayerDependencies>();
                if (deps != null)
                {
                    _playerControl = deps.PlayerControl;
                }
            }

            _uiController = localPlayer.GetComponentInChildren<UIController>(true);
            if (_uiController == null)
                _uiController = UIController.Instance;
        }

        private void ShowPopup()
        {
            ShowWindow();
            _playerControl?.LoseControl();
            _uiController?.UnlockMouse();
            PopulateGrid();
        }

        private void Close()
        {
            if (_targetInventory != null)
            {
                _targetInventory.OnInventoryChanged -= RefreshGrid;
                _targetInventory = null;
            }

            HideWindow();
            _playerControl?.CheckIfCanGrantControl();
            _uiController?.LockMouse();

            ClearSlots();
        }

        private void PopulateGrid()
        {
            ClearSlots();

            if (_targetInventory == null)
                return;

            foreach (var (itemId, amount) in _targetInventory.GetAllItems())
            {
                var itemData = ItemRegistry.GetItemData(itemId);
                var slot = Instantiate(slotPrefab, slotsContainer);
                slot.SetItem(itemId, itemData, amount);
                slot.OnSlotClicked += OnSlotClicked;
                _slotInstances.Add(slot);
            }
        }

        private void RefreshGrid()
        {
            if (_targetInventory != null)
            {
                PopulateGrid();
            }
        }

        private void OnSlotClicked(PeekInventorySlotUI slot)
        {
            if (_targetInventory == null || string.IsNullOrEmpty(slot.ItemId)) return;

            var localInventory = NetworkedPlayerInventory.FindByOwnerClientId(NetworkManager.Singleton.LocalClientId);
            if (localInventory == null) return;

            RemoveOrDecrementSlotInUI(slot);
            localInventory.RequestStealFrom(_targetInventory, slot.ItemId, 1);
            LogStealerInventory(localInventory);
        }

        private void RemoveOrDecrementSlotInUI(PeekInventorySlotUI slot)
        {
            if (slot.Amount <= 1)
            {
                slot.OnSlotClicked -= OnSlotClicked;
                _slotInstances.Remove(slot);
                Destroy(slot.gameObject);
            }
            else
            {
                var itemData = ItemRegistry.GetItemData(slot.ItemId);
                slot.SetItem(slot.ItemId, itemData, slot.Amount - 1);
            }
        }

        private void LogStealerInventory(NetworkedPlayerInventory stealerInventory)
        {
            var items = new List<string>();
            foreach (var (itemId, amount) in stealerInventory.GetAllItems())
            {
                var itemData = ItemRegistry.GetItemData(itemId);
                string name = itemData != null ? itemData.itemName : itemId;
                items.Add($"{name} x{amount}");
            }
            Debug.Log($"[PeekInventoryPopup] Stealer inventory: [{string.Join(", ", items)}]");
        }

        private void ClearSlots()
        {
            foreach (var slot in _slotInstances)
            {
                if (slot != null)
                {
                    slot.OnSlotClicked -= OnSlotClicked;
                    Destroy(slot.gameObject);
                }
            }
            _slotInstances.Clear();
        }

        private void OnDestroy()
        {
            if (_targetInventory != null)
            {
                _targetInventory.OnInventoryChanged -= RefreshGrid;
            }
        }

        private void HideWindow()
        {
            SetWindowVisible(false);
        }

        private void ShowWindow()
        {
            SetWindowVisible(true);
        }

        private void SetWindowVisible(bool visible)
        {
            overlayBackground.SetActive(visible);
            slotsContainer.parent.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Handles click on the overlay background to close the popup.
    /// </summary>
    public class PeekOverlayClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private System.Action _onClick;

        public void Init(System.Action onClick)
        {
            _onClick = onClick;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClick?.Invoke();
        }
    }
}
