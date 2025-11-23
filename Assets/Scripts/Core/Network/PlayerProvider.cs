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
    /// <summary>
    /// Manages player instantiation with per-player dependency injection scopes.
    /// Each player gets their own isolated SessionInventory and ItemDropper instances.
    /// </summary>
    public class PlayerProvider : IDisposable
    {
        private readonly IObjectResolver _parentResolver;
        private readonly ScatterConfigSO _scatterConfig;
        private readonly Dictionary<ulong, PlayerScope> _playerScopes;

        public PlayerProvider(IObjectResolver parentResolver, ScatterConfigSO scatterConfig)
        {
            _parentResolver = parentResolver ?? throw new ArgumentNullException(nameof(parentResolver));
            _scatterConfig = scatterConfig ?? throw new ArgumentNullException(nameof(scatterConfig));
            _playerScopes = new Dictionary<ulong, PlayerScope>();
        }

        /// <summary>
        /// Creates a new player instance with isolated per-player dependencies.
        /// </summary>
        /// <returns>Instantiated player GameObject with all dependencies injected</returns>
        public GameObject CreatePlayer(ulong clientId, GameObject playerPrefab, Vector3 position, Quaternion rotation)
        {
            if (playerPrefab == null)
                throw new ArgumentNullException(nameof(playerPrefab));

            if (_playerScopes.ContainsKey(clientId))
            {
                Debug.LogWarning($"[PlayerProvider] Player for client {clientId} already exists. Removing old player first.");
                RemovePlayer(clientId);
            }

            // Create a child scope for this specific player
            IScopedObjectResolver childScope = _parentResolver.CreateScope(builder =>
            {
                // Register per-player services (isolated instances for this player)
                builder.Register<SessionInventory>(Lifetime.Singleton);
                builder.Register<ItemDropper>(Lifetime.Singleton);

                // ScatterConfig is shared but needs to be available in child scope
                builder.RegisterInstance(_scatterConfig);

                // IMessageHub is inherited from parent scope (global singleton)
            });

            // Instantiate the player using the child scope's resolver
            // This ensures all components get dependencies from the child scope
            GameObject playerInstance = childScope.Instantiate(playerPrefab, position, rotation);

            // Get the PlayerController to pass to ItemDropper
            PlayerController playerController = playerInstance.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("[PlayerProvider] Player prefab must have a PlayerController component!");
                childScope.Dispose();
                UnityEngine.Object.Destroy(playerInstance);
                return null;
            }

            // Manually resolve and inject PlayerController into ItemDropper
            // (needed because ItemDropper constructor requires PlayerController reference)
            var sessionInventory = childScope.Resolve<SessionInventory>();
            var messageHub = childScope.Resolve<IMessageHub>();
            var itemDropper = new ItemDropper(sessionInventory, messageHub, playerController, _scatterConfig);

            // Store the player scope for cleanup later
            _playerScopes[clientId] = new PlayerScope
            {
                Scope = childScope,
                PlayerInstance = playerInstance,
                SessionInventory = sessionInventory,
                ItemDropper = itemDropper
            };

            Debug.Log($"[PlayerProvider] Player created for client {clientId} with isolated SessionInventory and ItemDropper.");
            return playerInstance;
        }

        public void RemovePlayer(ulong clientId)
        {
            if (!_playerScopes.TryGetValue(clientId, out PlayerScope playerScope))
            {
                Debug.LogWarning($"[PlayerProvider] No player found for client {clientId}");
                return;
            }

            Debug.Log($"[PlayerProvider] Removing player for client {clientId} and disposing per-player scope...");

            // Dispose ItemDropper (unsubscribes from events)
            playerScope.ItemDropper?.Dispose();

            // Destroy player GameObject
            if (playerScope.PlayerInstance != null)
            {
                UnityEngine.Object.Destroy(playerScope.PlayerInstance);
            }

            // Dispose the child scope (cleans up per-player services)
            playerScope.Scope?.Dispose();

            _playerScopes.Remove(clientId);
            Debug.Log($"[PlayerProvider] Player {clientId} removed successfully.");
        }

        public void Dispose()
        {
            foreach (var kvp in _playerScopes)
            {
                kvp.Value.ItemDropper?.Dispose();
                if (kvp.Value.PlayerInstance != null)
                {
                    UnityEngine.Object.Destroy(kvp.Value.PlayerInstance);
                }
                kvp.Value.Scope?.Dispose();
            }

            _playerScopes.Clear();
        }
    }
}
