using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;
using Arch.Core;
using System.Collections.Generic;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class ShapeComponentListener : ComponentListener<ShapeComponent>
    {
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private CourseVisualCatalog _catalog;
        private readonly Dictionary<CourseModel, CourseModelPresentation> _models = new();
        private CourseModelPresentation _model;
        private CourseModel _appliedModel;

        [Header("Built-in primitive meshes")]
        [SerializeField] private Mesh _cube;
        [SerializeField] private Mesh _sphere;
        [SerializeField] private Mesh _capsule;
        [SerializeField] private Mesh _cylinder;

        private Vector3 _localOffset;
        private World _world;
        private Entity _entity;

        public override void Sync(World world, Entity entity)
        {
            _world = world;
            _entity = entity;
            base.Sync(world, entity);
        }

        private MaterialPropertyBlock _propertyBlock;
        private PrimitiveShape _appliedShape = (PrimitiveShape)(-1);
        private Color _appliedTint = Color.clear;

        public override void UpdateView(in ShapeComponent component)
        {
            ApplyModel(component.Model);
            transform.localScale = component.Size;
            _localOffset = component.LocalOffset;
            transform.localPosition = _localOffset;

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

        private void ApplyModel(CourseModel model)
        {
            if (_appliedModel == model && (model == CourseModel.Primitive || _model != null)) return;
            if (_model != null) _model.gameObject.SetActive(false);
            _model = null;
            _appliedModel = model;
            if (model != CourseModel.Primitive && _catalog != null)
            {
                var entry = _catalog.Find(model);
                if (entry.Prefab != null)
                {
                    if (!_models.TryGetValue(model, out _model))
                    {
                        var instance = Instantiate(entry.Prefab, transform);
                        _model = instance.AddComponent<CourseModelPresentation>();
                        _models.Add(model, _model);
                    }
                    _model.gameObject.SetActive(true);
                    _model.Bind(_world, _entity, _catalog, entry);
                }
            }
            _meshRenderer.enabled = _model == null;
        }

        public override void Release() => _model?.ReleaseFeedback();

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
            if (_model != null) _model.gameObject.SetActive(false);
            _model = null;
            _appliedModel = CourseModel.Primitive;
            _meshRenderer.enabled = true;
            transform.localPosition = Vector3.zero;
            _appliedShape = (PrimitiveShape)(-1);
            _appliedTint = Color.clear;
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    }
}
