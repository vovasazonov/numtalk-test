using Unity.Mathematics;

namespace Project.GameDomain.Features.Physics.Scripts
{
    /// <summary>The swept character mover, as data. Its listener requires a CharacterController on the entity root.</summary>
    public struct CharacterBodyComponent
    {
        public float Height;
        public float Radius;
        public float3 Center;
        public float SlopeLimit;
        public float StepOffset;
        public float SkinWidth;
    }
}
