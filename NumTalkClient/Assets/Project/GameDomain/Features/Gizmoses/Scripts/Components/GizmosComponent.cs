using UnityEngine;

namespace Project.GameDomain.Features.Gizmoses.Scripts.Components
{
    public struct GizmosComponent
    {
        public GizmoShape Shape;
        public Color Color;
        public Vector3 Offset;
        public float Radius;
        public Vector3 Size;
        public bool IsWireframe;
    }
}
