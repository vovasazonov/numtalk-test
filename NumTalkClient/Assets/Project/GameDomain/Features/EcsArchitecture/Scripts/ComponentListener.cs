using System;
using System.Collections.Generic;
using Arch.Core;
using UnityEngine;

namespace Project.GameDomain.Features.EcsArchitecture.Scripts
{
    public abstract class ComponentListener : MonoBehaviour
    {
        private static readonly Type[] NoRootComponents = Array.Empty<Type>();

        public abstract Type ComponentType { get; }

        /// <summary>
        /// Unity components this listener needs on the entity root. The root adds one on first request and destroys
        /// it once the last listener that required it is released, so several listeners can share one Rigidbody.
        /// </summary>
        public virtual IReadOnlyList<Type> RequiredRootComponents => NoRootComponents;

        public abstract bool Matches(World world, Entity entity);

        public abstract void Sync(World world, Entity entity);
    }

    public abstract class ComponentListener<TComponent> : ComponentListener where TComponent : struct
    {
        public sealed override Type ComponentType => typeof(TComponent);

        public sealed override bool Matches(World world, Entity entity) => world.Has<TComponent>(entity);

        public sealed override void Sync(World world, Entity entity)
        {
            TComponent component = world.Get<TComponent>(entity);
            UpdateView(in component);
        }

        public abstract void UpdateView(in TComponent component);
    }
}
