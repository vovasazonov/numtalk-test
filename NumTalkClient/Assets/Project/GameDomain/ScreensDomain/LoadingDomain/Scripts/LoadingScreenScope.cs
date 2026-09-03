using Project.CoreDomain.Screen;
using VContainer;
using VContainer.Unity;

namespace Project.GameDomain.ScreensDomain.LoadingDomain.Scripts
{
    public class LoadingScreenScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<LoadingScreen>(Lifetime.Singleton).As<IScreen>();
        }
    }
}