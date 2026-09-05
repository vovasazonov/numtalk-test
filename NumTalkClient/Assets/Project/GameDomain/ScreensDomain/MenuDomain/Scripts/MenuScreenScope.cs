using Project.CoreDomain.Screen;
using Project.GameDomain.ScreensDomain.MenuDomain.Features.Ui.Scripts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.GameDomain.ScreensDomain.MenuDomain.Scripts
{
    public class MenuScreenScope : LifetimeScope
    {
        [SerializeField]  private MenuUiView _menuUiView;
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<MenuScreen>(Lifetime.Singleton).As<IScreen>();
            
            builder.RegisterComponent(_menuUiView).As<IMenuUiView>();
            builder.RegisterEntryPoint<MenuUiPresenter>(Lifetime.Singleton);
        }
    }
}
