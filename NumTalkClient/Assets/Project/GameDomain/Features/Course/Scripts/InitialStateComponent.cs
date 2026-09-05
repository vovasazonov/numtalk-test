using Unity.Mathematics;

namespace Project.GameDomain.Features.Course.Scripts
{
    /// <summary>Authored spawn pose, restored when a checkpoint snapshot is applied.</summary>
    public struct InitialStateComponent
    {
        public float3 Position;
        public quaternion Rotation;
    }
}
