using Arch.Core;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Player.Scripts
{
    /// <summary>Result of the bounded ground probe, not a raw CharacterController.isGrounded read.</summary>
    public struct GroundStateComponent
    {
        public bool IsGrounded;
        public float3 GroundNormal;
        public Entity GroundEntity;
    }
}
