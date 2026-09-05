using Unity.Mathematics;

namespace Project.GameDomain.Features.Player.Scripts
{
    /// <summary>Intrinsic velocity channel: thumb intent, gravity and jump. Metres per second.</summary>
    public struct PlayerMotorComponent
    {
        public float3 Velocity;
        public float3 PreviousPosition;
        public bool HasSimulationPose;
    }
}
