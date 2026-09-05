using Arch.Core;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Player.Scripts
{
    /// <summary>Platform velocity channel: surface motion inherited from the platform being ridden.</summary>
    public struct PlatformRiderComponent
    {
        public Entity Platform;
        public float3 SurfaceVelocity;
    }
}
