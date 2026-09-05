using Unity.Mathematics;

namespace Project.GameDomain.Features.Player.Scripts
{
    /// <summary>External velocity channel: knockback and other impulses, decayed independently.</summary>
    public struct ExternalVelocityComponent
    {
        public float3 Velocity;
    }
}
