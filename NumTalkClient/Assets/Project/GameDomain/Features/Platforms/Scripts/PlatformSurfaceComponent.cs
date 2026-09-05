using Unity.Mathematics;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    /// <summary>
    /// Shared surface contract for every platform. Behaviours (motion, ice, crumble) are separate components on the
    /// same entity, so a fourth behaviour is one component plus one system rather than a forked prefab family.
    /// </summary>
    public struct PlatformSurfaceComponent
    {
        /// <summary>Surface velocity handed to riders this tick, in metres per second.</summary>
        public float3 SurfaceVelocity;

        /// <summary>False while the surface is not standable, for example a crumble platform that has fallen away.</summary>
        public bool IsStandable;
    }
}
