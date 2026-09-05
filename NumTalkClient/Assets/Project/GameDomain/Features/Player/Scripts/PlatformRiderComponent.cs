using Arch.Core;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Player.Scripts
{
    /// <summary>Platform velocity channel: surface motion inherited from the platform being ridden.</summary>
    public struct PlatformRiderComponent
    {
        public Entity Platform;
        public float3 SurfaceVelocity;

        /// <summary>
        /// How much of the rider's intrinsic deceleration the surface removes. 0 is normal ground and 1 is
        /// frictionless, so the zero default is correct for a rider that is not standing on anything slick.
        /// </summary>
        public float SurfaceSlip;
    }
}
