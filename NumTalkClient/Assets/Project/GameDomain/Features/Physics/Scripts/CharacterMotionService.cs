using System.Collections.Generic;
using Arch.Core;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.Physics.Scripts
{
    /// <summary>View-owned physics bridge. ECS callers exchange values only, never Unity objects.</summary>
    public sealed class CharacterMotionService
    {
        private readonly Dictionary<Entity, CharacterBodyComponentListener> _bodies = new();
        public float3 CameraForward => Camera.main != null ? (float3)Camera.main.transform.forward : new float3(0f, 0f, 1f);
        public void Register(Entity entity, CharacterBodyComponentListener body) => _bodies[entity] = body;
        public void Unregister(Entity entity, CharacterBodyComponentListener body)
        {
            if (_bodies.TryGetValue(entity, out var current) && current == body) _bodies.Remove(entity);
        }
        public bool IsReady(Entity entity) => _bodies.ContainsKey(entity);
        public bool Probe(Entity entity, float distance, int mask, out float3 normal, out Entity ground)
            => _bodies[entity].Probe(distance, mask, out normal, out ground);
        public float3 Move(Entity entity, float3 position, float3 displacement, out bool below, out bool above)
            => _bodies[entity].Move(position, displacement, out below, out above);
        public IReadOnlyList<CharacterContact> DrainContacts(Entity entity) => _bodies[entity].DrainContacts();
    }
}
