using Unity.Mathematics;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    /// <summary>
    /// World pose owned by ECS. Its listener drives the entity root transform, which is what any Rigidbody or
    /// CharacterController on that root moves with.
    /// </summary>
    public struct EntityTransformComponent
    {
        public float3 Position;
        public quaternion Rotation;

        /// <summary>Unity layer applied to the entity root, so the collision matrix does the filtering.</summary>
        public int Layer;
    }
}
