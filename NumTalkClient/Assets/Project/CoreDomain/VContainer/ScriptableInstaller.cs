using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.CoreDomain.VContainer
{
    public abstract class ScriptableInstaller : ScriptableObject
    {
        public abstract void Install(IContainerBuilder builder, LifetimeScope scope);
    }
}
