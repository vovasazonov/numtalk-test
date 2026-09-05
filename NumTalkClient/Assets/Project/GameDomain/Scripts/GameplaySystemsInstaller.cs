using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.PlayerInput.Scripts;
using VContainer;
using Arch.Unity;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Physics.Scripts;

namespace Project.GameDomain.Scripts
{
    /// <summary>
    /// Owns the gameplay system schedule. EcsArchitectureInstaller creates the world and the view pipeline;
    /// everything the platformer simulates is registered here, feature by feature. Systems execute in registration
    /// order within each runner, so the order of the calls below is the execution order.
    /// </summary>
    public static class GameplaySystemsInstaller
    {
        public static void Install(IContainerBuilder builder, PlatformerTuningConfig tuning)
        {
            builder.RegisterInstance(tuning);
            builder.Register<CharacterMotionService>(Lifetime.Singleton);

            PlayerInputInstaller.InstallSampling(builder);
            InstallSimulation(builder);
        }

        /// <summary>
        /// Fixed 60 Hz simulation. Gameplay systems - motor, platform motion, collision resolution, projectiles,
        /// stomp, checkpoints - are appended here in intentional order as they land, and must be registered
        /// before the latch reset so they still see this tick's input edges.
        /// </summary>
        private static void InstallSimulation(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<PlayerMotorSystem>(SystemRunner.FixedUpdate);
            PlayerInputInstaller.InstallLatchReset(builder);
        }
    }
}
