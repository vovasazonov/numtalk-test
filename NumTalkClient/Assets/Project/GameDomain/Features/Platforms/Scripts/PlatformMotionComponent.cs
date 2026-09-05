using Unity.Mathematics;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    /// <summary>Authored two-point route, evaluated in fixed time by the moving platform system.</summary>
    public struct PlatformMotionComponent
    {
        public float3 StartPosition;
        public float3 EndPosition;

        /// <summary>Travel speed in metres per second.</summary>
        public float Speed;

        /// <summary>Pause at each end of the route, in seconds.</summary>
        public float WaitTime;

        /// <summary>Normalised position along the route, advanced by the system.</summary>
        public float Progress;

        /// <summary>Route direction: true while travelling towards <see cref="EndPosition"/>.</summary>
        public bool IsForward;

        /// <summary>Remaining pause at the current end of the route, in seconds.</summary>
        public float WaitTimer;
    }
}
