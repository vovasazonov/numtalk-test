using System.Collections.Generic;
using Project.CoreDomain.Screen;
using Project.CoreDomain.VContainer;
using Project.CoreDomain.View;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.Creature.Scripts;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.GameInput.Scripts;
using Project.GameDomain.Features.Input.Scripts;
using Project.GameDomain.Features.Jump.Scripts;
using Project.GameDomain.Features.Movement.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Pickup.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.ReapBehindPlayer.Scripts;
using Project.GameDomain.Features.Reaper.Scripts;
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
            ConfigsInstaller.Install(builder);
            PlayerInstaller.Install(builder);
            JumpInstaller.Install(builder);
            MovementInstaller.Install(builder);
            PhysicsInstaller.Install(builder);
            CreatureInstaller.Install(builder);
            ReaperInstaller.Install(builder);
            ReapBehindPlayerInstaller.Install(builder);
            PickupInstaller.Install(builder);
            InputInstaller.Install(builder);
            GameInputInstaller.Install(builder);
        }
    }
}
