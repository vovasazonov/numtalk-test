using Unity.Mathematics;

namespace Project.GameDomain.Features.Physics.Scripts
{
    /// <summary>
    /// The collision volume, as data. The listener owns the collider on its own child, so an entity root carrying a
    /// Rigidbody ends up with a compound body assembled from whichever collider components the entity has.
    /// </summary>
    public struct PhysicsColliderComponent
    {
        public ColliderShape Shape;
        public float3 Size;
        public bool IsTrigger;
    }
}
