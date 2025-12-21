using System;
using Assets.Scripts.Core.Player;
using Assets.Scripts.Core.Player.Character;
using Easy.MessageHub;
using HeistNSeek.Events;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace HeistNSeek.Core.Inventory.NetworkedInventory
{
    /// <summary>
    /// Handles dropping items when the player loses balance.
    /// Works with NetworkedPlayerInventory and NetworkedItemSpawnManager.
    /// Should be attached to the player prefab alongside NetworkedPlayerInventory.
    /// </summary>
    public class NetworkedItemDropper : NetworkBehaviour
    {
        private IMessageHub _messageHub;
        private NetworkedPlayerInventory _inventory;
        private Guid _balanceLostSubscription;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _inventory = GetComponent<NetworkedPlayerInventory>();
            if (_inventory == null)
            {
                Debug.LogError("[NetworkedItemDropper] NetworkedPlayerInventory not found on same GameObject!");
                return;
            }

            // Only owner subscribes to local balance events
            if (IsOwner && _messageHub != null)
            {
                _balanceLostSubscription = _messageHub.Subscribe<BalanceLostEvent>(OnBalanceLost);
                Debug.Log("[NetworkedItemDropper] Subscribed to BalanceLostEvent");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_messageHub != null && _balanceLostSubscription != Guid.Empty)
            {
                _messageHub.Unsubscribe(_balanceLostSubscription);
            }
            base.OnNetworkDespawn();
        }

        private void OnBalanceLost(BalanceLostEvent evt)
        {
            if (!IsOwner) return;

            Debug.Log($"[NetworkedItemDropper] Balance lost with impact {evt.ImpactSpeed}. Requesting drop.");

            // Request the server to drop all items
            Vector3 dropPosition = transform.position;
            _inventory.RequestDropAllItems(dropPosition, evt.ImpactSpeed);
        }

        /// <summary>
        /// Manually trigger item drop (for testing or other scenarios)
        /// </summary>
        public void DropAllItems(float impactSpeed = 1f)
        {
            if (_inventory == null) return;

            Vector3 dropPosition = transform.position;
            _inventory.RequestDropAllItems(dropPosition, impactSpeed);
        }
    }
}
