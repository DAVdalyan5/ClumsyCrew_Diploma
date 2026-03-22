using Assets.Scripts.Events.Inventory;
using Easy.MessageHub;
using HeistNSeek.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VContainer;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using TMPro;
using UnityEngine;
using Unity.Netcode;
using HeistNSeek.Core.Inventory.NetworkedInventory;
using Assets.Scripts.Runtime.Helpers;

namespace Assets.Scripts.UI.Views
{
    public class HeistEndView : UIView
    {
        [SerializeField] private GameEndStatItem playerStatsItemPrefab;
        [SerializeField] private Transform playerStatsContainer;

        private IMessageHub _messageHub;

        private Dictionary<string, int> playerStolenValues = new();

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        void Start()
        {
            BuildStolenValuesMap();
            foreach (var kvp in playerStolenValues)
            {
                var statItem = Instantiate(playerStatsItemPrefab, playerStatsContainer);
                statItem.SetData(kvp.Key, kvp.Value);
            }
        }

        public void BuildStolenValuesMap()
        {
            playerStolenValues.Clear();

            var allInventories = FindObjectsByType<NetworkedPlayerInventory>(FindObjectsSortMode.None);

            if (allInventories.IsNullOrEmpty())
            {
                return;
            }

            foreach (var inventory in allInventories)
            {
                int totalValue = 0;
                foreach (var (itemId, amount) in inventory.GetAllItems())
                {
                    var itemData = ItemRegistry.GetItemData(itemId);
                    if (itemData != null)
                        totalValue += itemData.price * amount;
                }

                string playerName = $"Player {inventory.OwnerClientId}";
                playerStolenValues[playerName] = totalValue;
            }
        }
    }
}
