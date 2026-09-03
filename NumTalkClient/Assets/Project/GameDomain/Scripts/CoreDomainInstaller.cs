using Project.CoreDomain.Camera.Scripts;
using Project.CoreDomain.Content;
using Project.CoreDomain.Data;
using Project.CoreDomain.Engine;
using Project.CoreDomain.FileLoader;
using Project.CoreDomain.Localization;
using Project.CoreDomain.Logger;
using Project.CoreDomain.Screen;
using Project.CoreDomain.Serialization;
using Project.CoreDomain.Time;
using VContainer;

namespace Project.GameDomain.Scripts
{
    public static class CoreDomainInstaller
    {

        public static void Install(IContainerBuilder builder)
        {
            builder.Register<LoggerService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterBuildCallback(resolver => resolver.Resolve<ILoggerService>());
            builder.Register<DataStorageService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<FileLoaderService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<SerializerService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ContentService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ScreensService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<EngineService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ApplicationPauseService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<CameraService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<TimeService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<LocalizationService>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
