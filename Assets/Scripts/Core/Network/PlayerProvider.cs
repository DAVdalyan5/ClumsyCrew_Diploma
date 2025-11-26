using System;
using System.Collections.Generic;
using Assets.Scripts.Core.Inventory.Models;
using Assets.Scripts.Core.Player;
using Easy.MessageHub;
using HeistNSeek.Core.Inventory.SessionInventory;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HeistNSeek.Core.Player
{
    public class PlayerProvider
    {
        private readonly IObjectResolver _parentResolver;
        private readonly ScatterConfigSO _scatterConfig;

        public PlayerProvider(IObjectResolver parentResolver, ScatterConfigSO scatterConfig)
        {
            _parentResolver = parentResolver ?? throw new ArgumentNullException(nameof(parentResolver));
            _scatterConfig = scatterConfig ?? throw new ArgumentNullException(nameof(scatterConfig));
        }

        public GameObject CreatePlayer(ulong clientId, GameObject playerPrefab, Vector3 position, Quaternion rotation)
        {
            GameObject playerInstance = _parentResolver.Instantiate(playerPrefab, position, rotation);
            return playerInstance;
        }
    }
}
