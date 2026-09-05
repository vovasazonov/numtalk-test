using Arch.Unity;
using Arch.Unity.Toolkit;
using VContainer;
using VContainer.Unity;

namespace Project.GameDomain.Features.PlayerInput.Scripts
{
    /// <summary>
    /// Registers the input feature. It installs in two parts because <see cref="InputLatchResetSystem"/> has to run
    /// at the end of the fixed tick, after every simulation system that consumes this tick's edges, while
    /// everything else belongs at the front of the frame.
    /// </summary>
    public static class PlayerInputInstaller
    {
        /// <summary>Render frame: sample the thumbs and the keyboard, and latch their edges for the next fixed tick.</summary>
        public static void InstallSampling(IContainerBuilder builder)
        {
            builder.Register<TouchPlayerInputService>(Lifetime.Singleton).AsSelf();
            builder.Register<KeyboardPlayerInputService>(Lifetime.Singleton).AsSelf();
            builder.Register<IPlayerInputSource>(resolver => new CompositePlayerInputSource(
                resolver.Resolve<TouchPlayerInputService>(),
                resolver.Resolve<KeyboardPlayerInputService>()), Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<TouchControlsView>().AsImplementedInterfaces();
            builder.RegisterSystemIntoArchApp<InputLatchSystem>(SystemRunner.Update);
        }

        /// <summary>Clears the consumed edges. Register this last in the fixed-step schedule.</summary>
        public static void InstallLatchReset(IContainerBuilder builder)
        {
            builder.RegisterSystemIntoArchApp<InputLatchResetSystem>(SystemRunner.FixedUpdate);
        }
    }
}
