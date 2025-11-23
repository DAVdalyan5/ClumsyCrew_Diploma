using Assets.Scripts.Core.Inventory.Models;
using Assets.Scripts.Runtime.Core;
using HeistNSeek.Core;
using HeistNSeek.Core.Player;
using NaughtyAttributes;
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
        builder.RegisterInstance(networkManager);

        // Testing services (can be removed when no longer needed)
        builder.Register<IPlainService, PlainService>(Lifetime.Singleton);
        builder.RegisterComponentInHierarchy<MonoService>();
        builder.RegisterComponentInHierarchy<InjectedConsumer>();

        // Player management - handles per-player dependency injection
        builder.Register<PlayerProvider>(Lifetime.Singleton);

        // Shared scene configuration
        builder.RegisterInstance(scatterConfigSO);
       // builder.RegisterInstance(playerSpawner);

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
