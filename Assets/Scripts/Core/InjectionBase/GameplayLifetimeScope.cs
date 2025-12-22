using Assets.Scripts.Core.Inventory.Models;
using Assets.Scripts.Runtime.Core;
using Easy.MessageHub;
using HeistNSeek.Core;
using HeistNSeek.Core.Inventory.SessionInventory;
using HeistNSeek.Core.Player;
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
    [SerializeField] private ScatterConfigSO scatterConfigSO;
    [SerializeField] private GameObject networkedItemSpawnManager;

    private LifetimeScope _parentScope;

    protected override void Awake()
    {
        // Find BootstrapLifetimeScope in the scene (should exist as DontDestroyOnLoad)
        _parentScope = FindAnyObjectByType<BootstrapLifetimeScope>();
        if (_parentScope == null)
        {
            Debug.LogWarning($"[GameplayLifetimeScope] BootstrapLifetimeScope not found. This scope will be root.");
        }
        else
        {
            Debug.Log($"[GameplayLifetimeScope] Parent scope set to BootstrapLifetimeScope.");
        }

        base.Awake();
    }

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(scatterConfigSO);

        builder.RegisterEntryPoint<GameplayEntryPoint>(Lifetime.Singleton).WithParameter(new EntryPointParameters(playerSpawnerPrefab, networkedItemSpawnManager));
    }

    #region Helper Methods
    [Button]
    public void SpawnPlayer()
    {
        //this.playerSpawner.SpawnPlayer((ulong)(UnityEngine.Random.value * 10));
    }
    #endregion
}
