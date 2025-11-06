using HeistNSeek.Core;
using VContainer;
using VContainer.Unity;

public class GameplayLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        //for testing purporses
        builder.Register<IPlainService, PlainService>(Lifetime.Singleton);
        builder.RegisterComponentInHierarchy<MonoService>();

        builder.RegisterComponentInHierarchy<InjectedConsumer>();
    }
}
