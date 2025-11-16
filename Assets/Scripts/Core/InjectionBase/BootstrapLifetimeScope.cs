using HeistNSeek.Core;
using HeistNSeek.Core.StateMachine;
using HeistNSeek.Core.StateMachine.States;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;
using Easy.MessageHub;
using Assets.Scripts.Core;
using R3;
using System.Collections.Generic;
using System;

public class BootstrapLifetimeScope : LifetimeScope
{
    [SceneDropdown]
    [SerializeField] private int gameplaySceneIndex = 0; // Default to index 1 (Main scene)

    protected override void Configure(IContainerBuilder builder)
    {
        // Register MessageHub for pub/sub messaging
        builder.Register<IMessageHub, MessageHub>(Lifetime.Singleton);

        // Register State Machine
        builder.Register<GameStateMachine>(Lifetime.Singleton);

        // Register all game states
        builder.Register<BootstrapState>(Lifetime.Singleton)
            .WithParameter(gameplaySceneIndex)
            .As<IState>();
        builder.Register<LoadingState>(Lifetime.Singleton).As<IState>();
        builder.Register<GameplayState>(Lifetime.Singleton).As<IState>();

        builder.Register<IGlobalTestService, GlobalTestService>(Lifetime.Singleton);
        builder.RegisterComponentInHierarchy<MonoGlobalService>().As<IMonoGlobalService>();

        // Register EntryPoint for Bootstrap initialization
        builder.RegisterEntryPoint<BootstrapEntryPoint>(Lifetime.Singleton).WithParameter(gameplaySceneIndex);
    }
}
