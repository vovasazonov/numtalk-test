using System.Collections.Generic;
using Project.CoreDomain.Screen;
using Project.CoreDomain.VContainer;
using Project.CoreDomain.View;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Scripts;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.GameDomain.ScreensDomain.ArenaDomain.Scripts
{
    public class ArenaScreenScope : LifetimeScope
    {
        [SerializeField] private List<ScriptableInstaller> _installers;

        protected override void Configure(IContainerBuilder builder)
        {
            _installers.ForEach(installer => installer.Install(builder, this));

            builder.Register<ArenaScreen>(Lifetime.Singleton).As<IScreen>();
            builder.Register<ArenaSceneLoader>(Lifetime.Singleton);
            builder.Register<ViewService>(Lifetime.Singleton).AsImplementedInterfaces();

            EcsArchitectureInstaller.Install(builder);
            GameplaySystemsInstaller.Install(builder);
        }
    }
}
