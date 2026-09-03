using Project.CoreDomain.Screen;
using VContainer;
using VContainer.Unity;

namespace Project.GameDomain.ScreensDomain.MenuDomain.Scripts
{
    public class MenuScreenScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<MenuScreen>(Lifetime.Singleton).As<IScreen>();
        }
    }
}
