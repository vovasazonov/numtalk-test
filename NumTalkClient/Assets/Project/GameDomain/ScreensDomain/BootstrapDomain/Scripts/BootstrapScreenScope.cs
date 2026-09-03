using System.Collections.Generic;
using Project.CoreDomain.Screen;
using Project.CoreDomain.VContainer;
using Project.CoreDomain.View;
using Project.GameDomain.Features.Bootstrap;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.GameDomain.ScreensDomain.BootstrapDomain.Scripts
{
    public class BootstrapScreenScope : LifetimeScope
    {
        [SerializeField] private List<ScriptableInstaller> _installers;
        [SerializeField] private BootstrapScreenContent _content;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<BootstrapScreen>(Lifetime.Singleton).As<IScreen>();
            builder.Register<BootstrapCommand>(Lifetime.Singleton).AsSelf();
            builder.RegisterInstance(_content);
            builder.Register<ViewService>(Lifetime.Singleton).AsImplementedInterfaces();

            foreach (var module in _installers)
            {
                module.Install(builder, this);
            }
        }
    }
}
