using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    public sealed class EntityTransformComponentListener : ComponentListener<EntityTransformComponent>
    {
        private int _appliedLayer = -1;

        public override void UpdateView(in EntityTransformComponent component)
        {
            Transform root = transform.parent;
            root.SetPositionAndRotation(component.Position, component.Rotation);

            if (_appliedLayer == component.Layer)
            {
                return;
            }

            _appliedLayer = component.Layer;
            root.gameObject.layer = component.Layer;
        }

        private void OnDisable()
        {
            _appliedLayer = -1;
        }
    }
}
