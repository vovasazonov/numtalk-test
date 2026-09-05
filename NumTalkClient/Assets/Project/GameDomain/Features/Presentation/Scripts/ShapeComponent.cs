using Unity.Mathematics;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    /// <summary>
    /// The visible primitive, as data. The course is built from Unity primitives, so the authored look survives
    /// ConvertAndDestroy without keeping the authoring GameObject alive.
    /// </summary>
    public struct ShapeComponent
    {
        public PrimitiveShape Shape;
        public float3 Size;
        public float3 LocalOffset;
        public float4 Tint;
    }
}
