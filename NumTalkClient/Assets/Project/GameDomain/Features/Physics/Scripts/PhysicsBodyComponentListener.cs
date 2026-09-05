using System;
using System.Collections.Generic;
using Arch.Core;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;
using VContainer;

namespace Project.GameDomain.Features.Physics.Scripts
{
    public sealed class PhysicsBodyComponentListener : ComponentListener<PhysicsBodyComponent>
    {
        private static readonly Type[] RootComponents = { typeof(Rigidbody) };

        private RigidBodyService _bodies;
        private Rigidbody _body;
        private Entity _entity;

        public override IReadOnlyList<Type> RequiredRootComponents => RootComponents;

        [Inject]
        public void Construct(RigidBodyService bodies) => _bodies = bodies;

        public override void Sync(World world, Entity entity)
        {
            base.Sync(world, entity);
            if (_body == null || _entity == entity) return;
            _entity = entity;
            _bodies.Register(entity, _body);
        }

        public override void Release()
        {
            if (_body != null) _bodies.Unregister(_entity, _body);
            _entity = default;
            _body = null;
        }

        private void OnDisable() => Release();

        public override void UpdateView(in PhysicsBodyComponent component)
        {
            Rigidbody body = transform.parent.GetComponent<Rigidbody>();
            if (body == null)
            {
                return;
            }

            _body = body;

            body.mass = component.Mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = component.FreezeRotation
                ? RigidbodyConstraints.FreezeRotation
                : RigidbodyConstraints.None;
        }
    }
}
