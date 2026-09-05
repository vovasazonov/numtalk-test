using Arch.Unity.Conversion;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    /// <summary>
    /// Captures the authored primitive so it can be rebuilt from ECS data after the authoring object is destroyed.
    /// Shape, size and colour are read from the renderer the artist actually edits, so there is nothing to keep in sync.
    /// </summary>
    public sealed class ShapeBaker : MonoBehaviour, IComponentConverter
    {
        [Tooltip("Renderer to bake. Defaults to this object's own mesh, or the first one found in children.")]
        [SerializeField] private MeshFilter _source;

        public void Convert(IEntityConverter converter)
        {
            MeshFilter source = ResolveSource();
            if (source == null)
            {
                Debug.LogWarning($"'{name}' has no mesh to bake. Skipping shape.", this);
                return;
            }

            Color tint = ReadTint(source);
            converter.AddComponent(new ShapeComponent
            {
                Shape = ReadShape(source),
                Size = source.transform.lossyScale,
                LocalOffset = Quaternion.Inverse(transform.rotation) * (source.transform.position - transform.position),
                Tint = new float4(tint.r, tint.g, tint.b, tint.a),
            });
        }

        private MeshFilter ResolveSource()
        {
            if (_source != null)
            {
                return _source;
            }

            return TryGetComponent(out MeshFilter own) ? own : GetComponentInChildren<MeshFilter>(true);
        }

        private PrimitiveShape ReadShape(MeshFilter source)
        {
            Mesh mesh = source.sharedMesh;
            if (mesh == null)
            {
                return PrimitiveShape.Cube;
            }

            switch (mesh.name)
            {
                case "Sphere": return PrimitiveShape.Sphere;
                case "Capsule": return PrimitiveShape.Capsule;
                case "Cylinder": return PrimitiveShape.Cylinder;
                case "Cube": return PrimitiveShape.Cube;
                default:
                    Debug.LogWarning($"'{name}' uses mesh '{mesh.name}', which is not a built-in primitive. Baking as a cube.", this);
                    return PrimitiveShape.Cube;
            }
        }

        private static Color ReadTint(MeshFilter source)
        {
            if (!source.TryGetComponent(out MeshRenderer renderer) || renderer.sharedMaterial == null)
            {
                return Color.white;
            }

            Material material = renderer.sharedMaterial;
            if (material.HasProperty(BaseColorId))
            {
                return material.GetColor(BaseColorId);
            }

            return material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white;
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
    }
}
