using System;
using System.Collections.Generic;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Physics.Scripts
{
    public sealed class PhysicsBodyComponentListener : ComponentListener<PhysicsBodyComponent>
    {
        private static readonly Type[] RootComponents = { typeof(Rigidbody) };

        public override IReadOnlyList<Type> RequiredRootComponents => RootComponents;

        public override void UpdateView(in PhysicsBodyComponent component)
        {
            Rigidbody body = transform.parent.GetComponent<Rigidbody>();
            if (body == null)
            {
                return;
            }

            body.mass = component.Mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = component.FreezeRotation
                ? RigidbodyConstraints.FreezeRotation
                : RigidbodyConstraints.None;
        }
    }
}
