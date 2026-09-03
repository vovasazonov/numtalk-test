using Arch.Unity;
using Project.CoreDomain.VContainer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.GameDomain.Features.Location.Scripts
{
    public class LocationInstaller : ScriptableInstaller
    {
        [SerializeField] private LocationTilesetDatabase _tilesetDatabase;

        public override void Install(IContainerBuilder builder, LifetimeScope scope)
        {
            builder.Register<LocationService>(Lifetime.Singleton).As<ILocationService>();
            builder.RegisterSystemIntoArchApp<LocationSystem>();
            builder.RegisterInstance(_tilesetDatabase);
        }
    }
}