using Assets.Scripts.Core.Inventory.Models;
using Assets.Scripts.Runtime.Core;
using Easy.MessageHub;
using HeistNSeek.Core;
using HeistNSeek.Core.Inventory.SessionInventory;
using HeistNSeek.UI;
using NaughtyAttributes;
using StarterAssets;
using System;
using System.Xml.Schema;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameplayLifetimeScope : LifetimeScope
{
    [SerializeField] private GameObject playerSpawnerPrefab;
    [Tooltip("Optional. World position and rotation for spawning the networked player (scene object in Main). If unset, players spawn at world origin.")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private ScatterConfigSO scatterConfigSO;
    [SerializeField] private GameObject networkedItemSpawnManager;
    [SerializeField] private GameObject cowsinsSessionServicesPrefab;

    protected override void Awake()
    {
        base.Awake();

        if (Parent is BootstrapLifetimeScope)
            Debug.Log("[GameplayLifetimeScope] Parent scope set to BootstrapLifetimeScope.");
        else if (Parent != null)
            Debug.Log($"[GameplayLifetimeScope] Parent scope set to {Parent.GetType().Name}.");
        else
            Debug.LogWarning("[GameplayLifetimeScope] BootstrapLifetimeScope not found. This scope will be root.");
    }

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(scatterConfigSO);
        builder.RegisterInstance(new PlayerSpawnContext(playerSpawnPoint));

        builder.RegisterEntryPoint<GameplayEntryPoint>(Lifetime.Singleton).WithParameter(new EntryPointParameters(playerSpawnerPrefab, networkedItemSpawnManager, cowsinsSessionServicesPrefab));
    }

    #region Helper Methods
    [Button]
    public void SpawnPlayer()
    {
        //this.playerSpawner.SpawnPlayer((ulong)(UnityEngine.Random.value * 10));
    }
    #endregion
}
