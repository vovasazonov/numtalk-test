using System;
using System.Collections.Generic;
using Arch.Core;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Project.GameDomain.Features.Physics.Scripts
{
    public sealed class CharacterBodyComponentListener : ComponentListener<CharacterBodyComponent>
    {
        private static readonly Type[] RootComponents = { typeof(CharacterController), typeof(CharacterContactRelay) };
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private CharacterMotionService _motion;
        private CharacterController _controller;
        private CharacterContactRelay _relay;
        private readonly List<CharacterContact> _contacts = new();
        private Entity _entity;
        private bool _registered;
        public override IReadOnlyList<Type> RequiredRootComponents => RootComponents;

        [Inject]
        public void Construct(CharacterMotionService motion) => _motion = motion;

        public override void Sync(World world, Entity entity)
        {
            base.Sync(world, entity);
            if (_registered) return;
            _entity = entity;
            var pose = world.Get<EntityTransformComponent>(entity);
            _controller.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            _controller.gameObject.layer = pose.Layer;
            _motion.Register(entity, this);
            _registered = true;
        }

        public override void UpdateView(in CharacterBodyComponent component)
        {
            if (_controller != null) return;
            _relay = transform.parent.GetComponent<CharacterContactRelay>();
            _controller = transform.parent.GetComponent<CharacterController>();
            _controller.height = component.Height;
            _controller.radius = component.Radius;
            _controller.center = component.Center;
            _controller.slopeLimit = component.SlopeLimit;
            _controller.stepOffset = component.StepOffset;
            _controller.skinWidth = component.SkinWidth;
            _controller.minMoveDistance = 0f;
        }

        public bool Probe(float distance, int mask, out float3 normal, out Entity ground)
        {
            normal = math.up();
            ground = default;
            float radius = _controller.radius * 0.9f;
            float lift = _controller.skinWidth + (_controller.radius - radius);
            Vector3 bottom = _controller.transform.TransformPoint(_controller.center)
                + Vector3.down * (_controller.height * 0.5f - _controller.radius);
            int count = UnityEngine.Physics.SphereCastNonAlloc(bottom + Vector3.up * lift, radius,
                Vector3.down, _hits, distance + lift + (_controller.radius - radius), mask, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            bool found = false;
            for (int index = 0; index < count; index++)
            {
                var hit = _hits[index];
                if (hit.collider.transform.IsChildOf(_controller.transform) ||
                    hit.normal.y < Mathf.Cos(_controller.slopeLimit * Mathf.Deg2Rad) || hit.distance >= nearest) continue;
                nearest = hit.distance;
                normal = hit.normal;
                var view = hit.collider.GetComponentInParent<EntityView>();
                ground = view != null ? view.Entity : default;
                found = true;
            }
            return found;
        }

        /// <summary>Contacts from the last <see cref="Move"/>, as values. Unity objects stop here.</summary>
        public IReadOnlyList<CharacterContact> DrainContacts()
        {
            _contacts.Clear();
            IReadOnlyList<CharacterContactRelay.Contact> hits = _relay.Contacts;
            for (int index = 0; index < hits.Count; index++)
            {
                CharacterContactRelay.Contact hit = hits[index];
                var view = hit.Collider.GetComponentInParent<EntityView>();
                if (view == null) continue;
                _contacts.Add(new CharacterContact
                {
                    Other = view.Entity,
                    Normal = hit.Normal,
                    Point = hit.Point,
                });
            }
            _relay.Clear();
            return _contacts;
        }

        public float3 Move(float3 position, float3 displacement, out bool below, out bool above)
        {
            _relay.Clear();
            // A restored ECS pose is authoritative even when the view was pooled or teleported.
            if (math.distancesq((float3)_controller.transform.position, position) > 0.000001f)
            {
                _controller.enabled = false;
                _controller.transform.position = position;
                _controller.enabled = true;
            }
            CollisionFlags flags = _controller.Move(displacement);
            below = (flags & CollisionFlags.Below) != 0;
            above = (flags & CollisionFlags.Above) != 0;
            return _controller.transform.position;
        }

        private void OnDisable() => Release();

        public override void Release()
        {
            if (_registered) _motion.Unregister(_entity, this);
            _registered = false;
            _controller = null;
        }
    }
}
