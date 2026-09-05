using Unity.Mathematics;

namespace Project.GameDomain.Features.Enemies.Scripts
{
    /// <summary>
    /// A pooled shooter projectile. It carries no Rigidbody or collider: the system sweeps its whole travel
    /// segment with a SphereCast every fixed step, so speed cannot make it tunnel.
    /// </summary>
    public struct ProjectileComponent
    {
        public float3 Velocity;
        public float Radius;
        public float RemainingLifeTime;
    }
}
