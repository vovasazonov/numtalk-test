using System.Collections.Generic;
using Arch.Core;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.Physics.Scripts
{
    /// <summary>
    /// View-owned bridge to the dynamic bodies, mirroring <see cref="CharacterMotionService"/>. ECS callers exchange
    /// values only. Physics is authoritative for a body's pose while it is simulating: systems read it back rather
    /// than writing it, and only <see cref="Teleport"/> overrides that, for a checkpoint restore.
    /// </summary>
    public sealed class RigidBodyService
    {
        private readonly Dictionary<Entity, Rigidbody> _bodies = new();

        public void Register(Entity entity, Rigidbody body) => _bodies[entity] = body;

        public void Unregister(Entity entity, Rigidbody body)
        {
            if (_bodies.TryGetValue(entity, out Rigidbody current) && current == body) _bodies.Remove(entity);
        }

        public bool IsReady(Entity entity) => _bodies.TryGetValue(entity, out Rigidbody body) && body != null;

        public void Read(Entity entity, out float3 position, out quaternion rotation, out float3 velocity)
        {
            Rigidbody body = _bodies[entity];
            position = body.position;
            rotation = body.rotation;
            velocity = body.linearVelocity;
        }

        /// <summary>Mass-independent horizontal push, so a heavier crate needs more force for the same shove.</summary>
        public void Accelerate(Entity entity, float3 acceleration)
            => _bodies[entity].AddForce(acceleration, ForceMode.Acceleration);

        public void Teleport(Entity entity, float3 position, quaternion rotation)
        {
            Rigidbody body = _bodies[entity];
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Move(position, rotation);
        }
    }
}
