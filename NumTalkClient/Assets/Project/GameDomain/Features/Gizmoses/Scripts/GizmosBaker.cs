using Arch.Unity.Conversion;
using Project.GameDomain.Features.Gizmoses.Scripts.Components;
using UnityEngine;

namespace Project.GameDomain.Features.Gizmoses.Scripts
{
    public sealed class GizmosBaker : MonoBehaviour, IComponentConverter
    {
        [SerializeField] private GizmoShape _shape = GizmoShape.Cube;
        [SerializeField] private Color _color = Color.cyan;
        [SerializeField] private Vector3 _size = new(0.25f, 0.25f, 0.25f);
        [SerializeField] private bool _isWireframe = true;

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new GizmosComponent
            {
                Shape = _shape,
                Color = _color,
                Offset = Vector3.zero,
                Radius = _size.x * 0.5f,
                Size = _size,
                IsWireframe = _isWireframe,
            });
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _color;

            switch (_shape)
            {
                case GizmoShape.Sphere:
                    if (_isWireframe)
                    {
                        Gizmos.DrawWireSphere(transform.position, _size.x * 0.5f);
                    }
                    else
                    {
                        Gizmos.DrawSphere(transform.position, _size.x * 0.5f);
                    }
                    break;
                case GizmoShape.Cube:
                    if (_isWireframe)
                    {
                        Gizmos.DrawWireCube(transform.position, _size);
                    }
                    else
                    {
                        Gizmos.DrawCube(transform.position, _size);
                    }
                    break;
            }
        }
    }
}
