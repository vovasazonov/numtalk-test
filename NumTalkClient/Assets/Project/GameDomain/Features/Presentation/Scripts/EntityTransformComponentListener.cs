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

            // Physics owns the pose of a simulating dynamic body, and ECS reads it back instead of writing it.
            // Writing here as well would make the two authorities fight every frame on the pushable crate.
            if (!root.TryGetComponent(out Rigidbody body) || body.isKinematic)
            {
                root.SetPositionAndRotation(component.Position, component.Rotation);
            }

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
