using Unity.Mathematics;

namespace Project.GameDomain.Features.Player.Scripts
{
    /// <summary>Last activated checkpoint. <see cref="CheckpointId"/> is zero while the run start is current.</summary>
    public struct CheckpointReferenceComponent
    {
        public int CheckpointId;
        public float3 RespawnPosition;
    }
}
