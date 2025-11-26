using Assets.Scripts.Core.Inventory.Models;
using Assets.Scripts.Runtime.Core;
using Easy.MessageHub;
using HeistNSeek.Core;
using HeistNSeek.Core.Inventory.SessionInventory;
using HeistNSeek.Core.Player;
using NaughtyAttributes;
using StarterAssets;
using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameplayLifetimeScope : LifetimeScope
{
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private ScatterConfigSO scatterConfigSO;
    [SerializeField] private NetworkManager networkManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IMessageHub, MessageHub>(Lifetime.Singleton);
        builder.RegisterInstance(networkManager);

        // Player management - handles per-player dependency injection
        builder.Register<PlayerProvider>(Lifetime.Singleton);

        // Shared scene configuration
        builder.RegisterInstance(scatterConfigSO);

        builder.Register<SessionInventory>(Lifetime.Singleton);

        // Register EntryPoint for Gameplay initialization
        builder.RegisterEntryPoint<GameplayEntryPoint>(Lifetime.Singleton);
    }

    #region Helper Methods
    [Button]
    public void SpawnPlayer()
    {
        this.playerSpawner.SpawnPlayer((ulong)(UnityEngine.Random.value * 10));
    }
    #endregion
}
