using HeistNSeek.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;
using Easy.MessageHub;
using Assets.Scripts.Core;

public class BootstrapLifetimeScope : LifetimeScope
{
    [SceneDropdown]
    [SerializeField] private int gameplaySceneIndex = 0; // Default to index 1 (Main scene)

    protected override void Configure(IContainerBuilder builder)
    {
        // Register MessageHub for pub/sub messaging
        builder.Register<IMessageHub, MessageHub>(Lifetime.Singleton);

        builder.Register<IGlobalTestService, GlobalTestService>(Lifetime.Singleton);
        builder.RegisterComponentInHierarchy<MonoGlobalService>().As<IMonoGlobalService>();

        // Register EntryPoint for Bootstrap initialization
        builder.RegisterEntryPoint<BootstrapEntryPoint>(Lifetime.Singleton).WithParameter(gameplaySceneIndex);
    }
}
