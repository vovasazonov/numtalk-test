using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.PlayerInput.Scripts;
using VContainer;
using Project.GameDomain.Features.CameraControl.Scripts;
using Arch.Unity;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Pushables.Scripts;
using Project.GameDomain.Features.Enemies.Scripts;
using Project.GameDomain.Features.Course.Scripts;

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
            builder.Register<RigidBodyService>(Lifetime.Singleton);
            builder.Register<ProjectilePool>(Lifetime.Singleton);
            builder.Register<CourseSnapshotService>(Lifetime.Singleton);

            PlayerInputInstaller.InstallSampling(builder);
            InstallSimulation(builder);
            builder.Register<CourseCameraPresentation>(Lifetime.Singleton);
            builder.RegisterSystemIntoArchApp<CameraFollowSystem>(SystemRunner.PreLateUpdate);
        }

        /// <summary>
        /// Fixed 60 Hz simulation. Gameplay systems - motor, platform motion, collision resolution, projectiles,
        /// stomp, checkpoints - are appended here in intentional order as they land, and must be registered
        /// before the latch reset so they still see this tick's input edges.
        /// </summary>
        private static void InstallSimulation(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<MovingPlatformSystem>(SystemRunner.FixedUpdate);
            builder.RegisterSystemIntoArchApp<CrumblePlatformSystem>(SystemRunner.FixedUpdate);
            builder.RegisterSystemIntoArchApp<PlatformRiderSystem>(SystemRunner.FixedUpdate);
            builder.RegisterSystemIntoArchApp<PlayerMotorSystem>(SystemRunner.FixedUpdate);
            // After the motor, so the shove uses this tick's contacts and the read-back sees the resulting pose.
            builder.RegisterSystemIntoArchApp<CratePushSystem>(SystemRunner.FixedUpdate);
            builder.RegisterSystemIntoArchApp<PushableBodySystem>(SystemRunner.FixedUpdate);
            // Enemies move first, then shoot from the pose they just reached, then this tick's projectiles sweep.
            builder.RegisterSystemIntoArchApp<EnemyPatrolSystem>(SystemRunner.FixedUpdate);
            builder.RegisterSystemIntoArchApp<ShooterSystem>(SystemRunner.FixedUpdate);
            builder.RegisterSystemIntoArchApp<ProjectileSystem>(SystemRunner.FixedUpdate);
            // After the motor, because the stomp is judged from the segment the player just travelled.
            builder.RegisterSystemIntoArchApp<StompSystem>(SystemRunner.FixedUpdate);
            // Last, so a life lost this tick resolves after every system that could have taken it.
            builder.RegisterSystemIntoArchApp<CourseTriggerSystem>(SystemRunner.FixedUpdate);
            builder.RegisterSystemIntoArchApp<RespawnSystem>(SystemRunner.FixedUpdate);
            PlayerInputInstaller.InstallLatchReset(builder);
        }
    }
}
