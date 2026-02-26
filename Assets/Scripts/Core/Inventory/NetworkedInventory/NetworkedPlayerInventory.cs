using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Core.Inventory.Models;
using Assets.Scripts.Events.Inventory;
using Easy.MessageHub;
using HeistNSeek.Events;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace HeistNSeek.Core.Inventory.NetworkedInventory
{
    /// <summary>
    /// Per-player inventory that syncs across the network.
    /// Attached to each player prefab - each player has their own instance.
    /// Server-authoritative: only server modifies inventory, clients see synced state.
    /// </summary>
    public class NetworkedPlayerInventory : NetworkBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxSlots = 20;

        private IMessageHub _messageHub;

        // Server-side inventory storage (only valid on server/host)
        private readonly List<InventoryEntry> _serverItems = new();

        // Client-side synced list for UI/display purposes
        private readonly NetworkList<NetworkedInventorySlot> _syncedInventory = new();

        /// <summary>
        /// Event fired when inventory changes (client-side for UI updates)
        /// </summary>
        public event Action OnInventoryChanged;

        //public int TotalItemCount => _syncedInventory.Sum(slot => slot.Amount);
        public int SlotCount => _syncedInventory.Count;
        public int MaxSlots => maxSlots;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        private void Awake()
        {
            _syncedInventory.OnListChanged += OnSyncedInventoryChanged;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log($"[NetworkedPlayerInventory] Spawned for client {OwnerClientId}, IsOwner: {IsOwner}, IsServer: {IsServer}");
        }

        public override void OnDestroy()
        {
            _syncedInventory.OnListChanged -= OnSyncedInventoryChanged;
            base.OnDestroy();
        }

        private void OnSyncedInventoryChanged(NetworkListEvent<NetworkedInventorySlot> changeEvent)
        {
            OnInventoryChanged?.Invoke();

            if (IsOwner && _messageHub != null)
            {
                // Notify local systems about inventory change
                Debug.Log($"[NetworkedPlayerInventory] Inventory changed: {changeEvent.Type}");
                
                //TODO: Separte This, create extension to iterate throught this shit.
                int inventoryPrice = 0;
                foreach (var item in _syncedInventory)
                {
                    var itemData = ItemRegistry.GetItemData(item.ItemId.ToString());
                    if (itemData != null)
                    {
                        inventoryPrice += itemData.price * item.Amount;
                    }
                }

                _messageHub.Publish(new InventoryPriceChangedEvent(inventoryPrice));
            }
        }

        /// <summary>
        /// Request to add an item. Can be called by anyone but only server processes.
        /// </summary>
        public void RequestAddItem(string itemId, int amount = 1)
        {
            if (IsServer)
            {
                ServerAddItem(itemId, amount);
            }
            else
            {
                AddItemServerRpc(itemId, amount);
            }
        }

        /// <summary>
        /// Request to remove an item. Can be called by anyone but only server processes.
        /// </summary>
        public void RequestRemoveItem(string itemId, int amount = 1)
        {
            if (IsServer)
            {
                ServerRemoveItem(itemId, amount);
            }
            else
            {
                RemoveItemServerRpc(itemId, amount);
            }
        }

        /// <summary>
        /// Request to drop all items. Used when player loses balance.
        /// </summary>
        public void RequestDropAllItems(Vector3 dropPosition, float impactSpeed)
        {
            if (IsServer)
            {
                ServerDropAllItems(dropPosition, impactSpeed);
            }
            else
            {
                DropAllItemsServerRpc(dropPosition, impactSpeed);
            }
        }

        [Rpc(SendTo.Server)]
        private void AddItemServerRpc(string itemId, int amount)
        {
            ServerAddItem(itemId, amount);
        }

        [Rpc(SendTo.Server)]
        private void RemoveItemServerRpc(string itemId, int amount)
        {
            ServerRemoveItem(itemId, amount);
        }

        [Rpc(SendTo.Server)]
        private void DropAllItemsServerRpc(Vector3 dropPosition, float impactSpeed)
        {
            ServerDropAllItems(dropPosition, impactSpeed);
        }

        /// <summary>
        /// Server-side: Add item to inventory
        /// </summary>
        internal void ServerAddItem(string itemId, int amount)
        {
            if (!IsServer) return;

            // Find existing slot with same item
            for (int i = 0; i < _syncedInventory.Count; i++)
            {
                var slot = _syncedInventory[i];
                if (slot.ItemId.ToString() == itemId)
                {
                    // Update existing slot
                    var updatedSlot = new NetworkedInventorySlot
                    {
                        ItemId = slot.ItemId,
                        Amount = slot.Amount + amount
                    };
                    _syncedInventory[i] = updatedSlot;

                    Debug.Log($"[NetworkedPlayerInventory] Server: Updated {itemId} to {updatedSlot.Amount} for client {OwnerClientId}");
                    NotifyItemAddedClientRpc(itemId, amount, updatedSlot.Amount);
                    return;
                }
            }

            // Create new slot
            if (_syncedInventory.Count < maxSlots)
            {
                var newSlot = new NetworkedInventorySlot
                {
                    ItemId = itemId,
                    Amount = amount
                };
                _syncedInventory.Add(newSlot);

                Debug.Log($"[NetworkedPlayerInventory] Server: Added new {itemId} x{amount} for client {OwnerClientId}");
                NotifyItemAddedClientRpc(itemId, amount, amount);
            }
            else
            {
                Debug.LogWarning($"[NetworkedPlayerInventory] Inventory full for client {OwnerClientId}");
            }
        }

        /// <summary>
        /// Server-side: Remove item from inventory
        /// </summary>
        internal void ServerRemoveItem(string itemId, int amount)
        {
            if (!IsServer) return;

            for (int i = _syncedInventory.Count - 1; i >= 0; i--)
            {
                var slot = _syncedInventory[i];
                if (slot.ItemId.ToString() == itemId)
                {
                    int newAmount = slot.Amount - amount;

                    if (newAmount <= 0)
                    {
                        _syncedInventory.RemoveAt(i);
                        Debug.Log($"[NetworkedPlayerInventory] Server: Removed all {itemId} from client {OwnerClientId}");
                    }
                    else
                    {
                        var updatedSlot = new NetworkedInventorySlot
                        {
                            ItemId = slot.ItemId,
                            Amount = newAmount
                        };
                        _syncedInventory[i] = updatedSlot;
                        Debug.Log($"[NetworkedPlayerInventory] Server: Reduced {itemId} to {newAmount} for client {OwnerClientId}");
                    }

                    NotifyItemRemovedClientRpc(itemId, amount, Mathf.Max(0, newAmount));
                    return;
                }
            }
        }

        /// <summary>
        /// Server-side: Drop all items when player loses balance
        /// </summary>
        private void ServerDropAllItems(Vector3 dropPosition, float impactSpeed)
        {
            if (!IsServer) return;

            // Use server-side position for accuracy (client position may be outdated due to latency)
            Vector3 serverDropPosition = transform.position;

            var itemsToDrop = new List<string>();
            var amounts = new List<int>();

            // Collect all items to drop
            foreach (var slot in _syncedInventory)
            {
                itemsToDrop.Add(slot.ItemId.ToString());
                amounts.Add(slot.Amount);
            }

            // Clear the inventory
            _syncedInventory.Clear();

            // Spawn items in world via the item spawn manager
            if (NetworkedItemSpawnManager.Instance != null)
            {
                for (int i = 0; i < itemsToDrop.Count; i++)
                {
                    for (int j = 0; j < amounts[i]; j++)
                    {
                        NetworkedItemSpawnManager.Instance.SpawnItemInWorld(
                            itemsToDrop[i],
                            serverDropPosition,
                            impactSpeed
                        );
                    }
                }
            }

            Debug.Log($"[NetworkedPlayerInventory] Server: Dropped {itemsToDrop.Count} item types for client {OwnerClientId}");
            NotifyItemsDroppedClientRpc(serverDropPosition, impactSpeed);
        }

        [Rpc(SendTo.Owner)]
        private void NotifyItemAddedClientRpc(string itemId, int amount, int totalCount)
        {
            if (_messageHub != null)
            {
                var itemData = ItemRegistry.GetItemData(itemId);
                if (itemData != null)
                {
                    _messageHub.Publish(new ItemAddedEvent(itemData, amount, totalCount));
                }
            }
        }

        [Rpc(SendTo.Owner)]
        private void NotifyItemRemovedClientRpc(string itemId, int amount, int remainingCount)
        {
            if (_messageHub != null)
            {
                var itemData = ItemRegistry.GetItemData(itemId);
                if (itemData != null)
                {
                    _messageHub.Publish(new ItemRemovedEvent(itemData, amount, remainingCount));
                }
            }
        }

        [Rpc(SendTo.Owner)]
        private void NotifyItemsDroppedClientRpc(Vector3 dropPosition, float impactSpeed)
        {
            if (_messageHub != null)
            {
                _messageHub.Publish(new ItemsDroppedEvent(new List<ItemDataSO>(), dropPosition, impactSpeed));
            }
        }

        /// <summary>
        /// Get count of a specific item (client-side query)
        /// </summary>
        public int GetItemCount(string itemId)
        {
            foreach (var slot in _syncedInventory)
            {
                if (slot.ItemId.ToString() == itemId)
                {
                    return slot.Amount;
                }
            }
            return 0;
        }

        /// <summary>
        /// Check if has at least specified amount of item
        /// </summary>
        public bool HasItem(string itemId, int amount = 1)
        {
            return GetItemCount(itemId) >= amount;
        }

        /// <summary>
        /// Get all inventory slots (for UI display)
        /// </summary>
        public IEnumerable<(string itemId, int amount)> GetAllItems()
        {
            foreach (var slot in _syncedInventory)
            {
                yield return (slot.ItemId.ToString(), slot.Amount);
            }
        }

        /// <summary>
        /// Request to steal an item from another player's inventory.
        /// Called by the local player (the stealer). Server-authoritative.
        /// </summary>
        public void RequestStealFrom(NetworkedPlayerInventory target, string itemId, int amount = 1)
        {
            if (!IsOwner) return;
            if (target == null) return;
            if (target.OwnerClientId == OwnerClientId) return; // Cannot steal from self

            RequestStealFromServerRpc(target.OwnerClientId, itemId, amount);
        }

        [ServerRpc]
        private void RequestStealFromServerRpc(ulong targetOwnerClientId, string itemId, int amount, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            ulong requesterClientId = rpcParams.Receive.SenderClientId;
            if (requesterClientId == targetOwnerClientId) return;

            var targetInventory = FindPlayerInventory(targetOwnerClientId);
            var requesterInventory = FindPlayerInventory(requesterClientId);

            if (targetInventory == null || requesterInventory == null) return;

            int available = targetInventory.GetItemCount(itemId);
            int toSteal = Mathf.Min(amount, available);
            if (toSteal <= 0) return;

            targetInventory.ServerRemoveItem(itemId, toSteal);
            requesterInventory.ServerAddItem(itemId, toSteal);

            Debug.Log($"[NetworkedPlayerInventory] Server: Stole {toSteal}x {itemId} from {targetOwnerClientId} to {requesterClientId}");
        }

        private static NetworkedPlayerInventory FindPlayerInventory(ulong clientId)
        {
            return FindByOwnerClientId(clientId);
        }

        /// <summary>
        /// Find a player's inventory by their network client ID.
        /// </summary>
        public static NetworkedPlayerInventory FindByOwnerClientId(ulong clientId)
        {
            var players = FindObjectsByType<NetworkedPlayerInventory>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.OwnerClientId == clientId)
                    return player;
            }
            return null;
        }

        /// <summary>
        /// Internal class for server-side storage
        /// </summary>
        private class InventoryEntry
        {
            public string ItemId;
            public int Amount;
        }
    }
}
