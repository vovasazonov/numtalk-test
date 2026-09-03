using Arch.Core;
using UnityEngine;

namespace Project.GameDomain.Features.EcsArchitecture.Scripts
{
    public abstract class ComponentListener : MonoBehaviour
    {
        public abstract System.Type ComponentType { get; }

        public abstract bool Matches(World world, Entity entity);

        public abstract void Sync(World world, Entity entity);
    }

    public abstract class ComponentListener<TComponent> : ComponentListener where TComponent : struct
    {
        public sealed override System.Type ComponentType => typeof(TComponent);

        public sealed override bool Matches(World world, Entity entity) => world.Has<TComponent>(entity);

        public sealed override void Sync(World world, Entity entity)
        {
            TComponent component = world.Get<TComponent>(entity);
            UpdateView(in component);
        }

        public abstract void UpdateView(in TComponent component);
    }
}