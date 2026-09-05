using Unity.Mathematics;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    /// <summary>
    /// Visible geometry as values: a primitive fallback or an optional catalog model, size, offset and tint.
    /// The authored look survives ConvertAndDestroy without Unity references in ECS state.
    /// </summary>
    public struct ShapeComponent
    {
        public CourseModel Model;
        public PrimitiveShape Shape;
        public float3 Size;
        public float3 LocalOffset;
        public float4 Tint;
    }
}
