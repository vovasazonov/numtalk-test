using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Gizmoses.Scripts.Components;
using UnityEngine;

namespace Project.GameDomain.Features.Gizmoses.Scripts.Listeners
{
    public class GizmosComponentListener : ComponentListener<GizmosComponent>
    {
        private GizmosComponent _component;
        private bool _hasData;

        public override void UpdateView(in GizmosComponent component)
        {
            _component = component;
            _hasData = true;
        }

        private void OnDrawGizmos()
        {
            if (!_hasData)
            {
                return;
            }

            Gizmos.color = _component.Color;
            Vector3 center = transform.position + _component.Offset;

            switch (_component.Shape)
            {
                case GizmoShape.Sphere:
                    if (_component.IsWireframe)
                    {
                        Gizmos.DrawWireSphere(center, _component.Radius);
                    }
                    else
                    {
                        Gizmos.DrawSphere(center, _component.Radius);
                    }
                    break;
                case GizmoShape.Cube:
                    if (_component.IsWireframe)
                    {
                        Gizmos.DrawWireCube(center, _component.Size);
                    }
                    else
                    {
                        Gizmos.DrawCube(center, _component.Size);
                    }
                    break;
            }
        }
    }
}
