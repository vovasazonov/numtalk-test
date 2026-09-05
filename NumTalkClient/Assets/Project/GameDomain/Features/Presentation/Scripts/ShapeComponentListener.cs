using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class ShapeComponentListener : ComponentListener<ShapeComponent>
    {
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;

        [Header("Built-in primitive meshes")]
        [SerializeField] private Mesh _cube;
        [SerializeField] private Mesh _sphere;
        [SerializeField] private Mesh _capsule;
        [SerializeField] private Mesh _cylinder;

        private MaterialPropertyBlock _propertyBlock;
        private PrimitiveShape _appliedShape = (PrimitiveShape)(-1);
        private Color _appliedTint = Color.clear;

        public override void UpdateView(in ShapeComponent component)
        {
            transform.localScale = component.Size;

            if (_appliedShape != component.Shape)
            {
                _appliedShape = component.Shape;
                _meshFilter.sharedMesh = ResolveMesh(component.Shape);
            }

            Color tint = new(component.Tint.x, component.Tint.y, component.Tint.z, component.Tint.w);
            if (_appliedTint == tint)
            {
                return;
            }

            _appliedTint = tint;
            _propertyBlock ??= new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, tint);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        private Mesh ResolveMesh(PrimitiveShape shape)
        {
            return shape switch
            {
                PrimitiveShape.Sphere => _sphere,
                PrimitiveShape.Capsule => _capsule,
                PrimitiveShape.Cylinder => _cylinder,
                _ => _cube,
            };
        }

        private void OnDisable()
        {
            _appliedShape = (PrimitiveShape)(-1);
            _appliedTint = Color.clear;
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    }
}
