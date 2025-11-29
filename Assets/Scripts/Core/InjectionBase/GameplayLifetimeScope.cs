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
using Unity.VisualScripting;
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

        //RegisterSpawner(builder);

        builder.RegisterEntryPoint<GameplayEntryPoint>(Lifetime.Singleton);
    }

    private void RegisterSpawner(IContainerBuilder builder)
    {
        //workaround for the reference bug of multiplayer center
        if (playerSpawner == null)
        {
            this.playerSpawner = FindFirstObjectByType<PlayerSpawner>();
        }

        builder.RegisterComponent(playerSpawner);
    }

    #region Helper Methods
    [Button]
    public void SpawnPlayer()
    {
        this.playerSpawner.SpawnPlayer((ulong)(UnityEngine.Random.value * 10));
    }
    #endregion
}
