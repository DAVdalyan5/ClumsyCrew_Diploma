using Assets.Scripts.Core.Inventory.Models;
using Assets.Scripts.Core.Player;
using HeistNSeek.Core;
using HeistNSeek.Core.Inventory.SessionInventory;
using HeistNSeek.Core.Player;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameplayLifetimeScope : LifetimeScope
{
    [SerializeField] private ScatterConfigSO scatterConfigSO;

    protected override void Configure(IContainerBuilder builder)
    {
        //for testing purporses
        builder.Register<IPlainService, PlainService>(Lifetime.Singleton);
        builder.RegisterComponentInHierarchy<MonoService>();

        builder.RegisterComponentInHierarchy<InjectedConsumer>();

        // Register PlayerController for IMessageHub injection
        builder.RegisterComponentInHierarchy<PlayerController>();
        // Register First Person Input and Movement
        builder.RegisterComponentInHierarchy<FirstPersonInputService>();
        builder.RegisterComponentInHierarchy<FirstPersonMovementHandler>();

        // Register Session Inventory System
        builder.Register<SessionInventory>(Lifetime.Singleton);
        builder.Register<ItemDropper>(Lifetime.Singleton);
        builder.Register<ItemPickup>(Lifetime.Transient);

        builder.RegisterInstance<ScatterConfigSO>(scatterConfigSO);

        // Register EntryPoint for Gameplay initialization
        builder.RegisterEntryPoint<GameplayEntryPoint>(Lifetime.Singleton);
    }
}
