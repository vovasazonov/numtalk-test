using Arch.Unity;
using Arch.Unity.Toolkit;
using Project.CoreDomain.Lifecycle;
using Project.GameDomain.Features.PlayerInput.Scripts;
using VContainer;

namespace Project.GameDomain.Features.EcsArchitecture.Scripts
{
    /// <summary>
    /// Owns the world and the system schedule. Systems execute in registration order within each runner, so the
    /// order of the calls below is the execution order.
    /// </summary>
    public static class EcsArchitectureInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.UseNewArchApp(Lifetime.Singleton, _ => { });
            builder.Register<ComponentListenerRegistry>(Lifetime.Singleton).AsSelf().As<ITaskAsyncInitializable>();
            builder.Register<NullPlayerInputSource>(Lifetime.Singleton).As<IPlayerInputSource>();

            InstallInputSampling(builder);
            InstallSimulation(builder);
            InstallPresentation(builder);
        }

        /// <summary>Render frame: sample the thumbs and latch their edges for the next fixed tick.</summary>
        private static void InstallInputSampling(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<InputLatchSystem>(SystemRunner.Update);
        }

        /// <summary>
        /// Fixed 60 Hz simulation. Gameplay systems - motor, platform motion, collision resolution, projectiles,
        /// stomp, checkpoints - are appended here in intentional order as they land, and must be registered
        /// <b>before</b> <see cref="InputLatchResetSystem"/> so they still see this tick's input edges.
        /// </summary>
        private static void InstallSimulation(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<InputLatchResetSystem>(SystemRunner.FixedUpdate);
        }

        /// <summary>Presentation: rebuild and project views after the simulation has settled for the frame.</summary>
        private static void InstallPresentation(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<ViewSystem>(SystemRunner.PreLateUpdate);
        }
    }
}
