using System;
using System.Collections.Generic;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Pushables.Scripts
{
    /// <summary>
    /// Second user of the root Rigidbody: a pushable needs a body to receive impulses, and so does
    /// <c>PhysicsBodyComponent</c>. The entity root reference-counts the Rigidbody, so removing either component
    /// leaves it in place and removing both destroys it.
    /// </summary>
    public sealed class PushableComponentListener : ComponentListener<PushableComponent>
    {
        private static readonly Type[] RootComponents = { typeof(Rigidbody) };

        public override IReadOnlyList<Type> RequiredRootComponents => RootComponents;

        public override void UpdateView(in PushableComponent component)
        {
        }
    }
}
