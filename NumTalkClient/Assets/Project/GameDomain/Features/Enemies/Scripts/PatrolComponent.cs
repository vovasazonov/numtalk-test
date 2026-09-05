using Unity.Mathematics;

namespace Project.GameDomain.Features.Enemies.Scripts
{
    /// <summary>Authored two-point patrol route, evaluated in fixed time.</summary>
    public struct PatrolComponent
    {
        public float3 StartPosition;
        public float3 EndPosition;
        public float Speed;
        public float WaitTime;
        public bool IsForward;
        public float WaitTimer;
    }
}
