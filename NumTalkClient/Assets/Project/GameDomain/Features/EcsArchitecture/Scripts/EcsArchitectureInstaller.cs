using Arch.Unity;
using Project.CoreDomain.Lifecycle;
using VContainer;

namespace Project.GameDomain.Features.EcsArchitecture.Scripts
{
    public static class EcsArchitectureInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.UseNewArchApp(Lifetime.Singleton, _ => { });
            builder.Register<ComponentListenerRegistry>(Lifetime.Singleton).AsSelf().As<ITaskAsyncInitializable>();
            builder.RegisterSystemIntoArchApp<ViewSystem>();
        }
    }
}