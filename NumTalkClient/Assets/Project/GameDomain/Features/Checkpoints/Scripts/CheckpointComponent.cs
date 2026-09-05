using Unity.Mathematics;

namespace Project.GameDomain.Features.Checkpoints.Scripts
{
    public struct CheckpointComponent
    {
        /// <summary>Authored order along the course. Higher ids never regress to a lower one.</summary>
        public int Id;
        public float3 RespawnPosition;
        public bool IsActivated;
    }
}
