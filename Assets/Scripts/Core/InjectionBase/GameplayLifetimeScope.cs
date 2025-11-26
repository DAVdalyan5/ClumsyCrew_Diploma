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

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(scatterConfigSO);
        builder.Register<IMessageHub, MessageHub>(Lifetime.Singleton);

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
