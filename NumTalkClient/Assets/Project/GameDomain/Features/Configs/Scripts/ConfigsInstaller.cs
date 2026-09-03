using VContainer;

namespace Project.GameDomain.Features.Configs.Scripts
{
    public static class ConfigsInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<ConfigService>(Lifetime.Singleton).As<IConfigService>();
        }
    }
}
