using Project.CoreDomain.VContainer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.GameDomain.Scripts
{
    public class GameDomainInstaller : ScriptableInstaller
    {
        [SerializeField] private ScreensDomainInstaller _screensDomainInstaller;
        
        public override void Install(IContainerBuilder builder, LifetimeScope scope)
        {
            CoreDomainInstaller.Install(builder);
            _screensDomainInstaller.Install(builder, scope);
        }
    }
}
