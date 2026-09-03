using Project.CoreDomain.Camera.Scripts;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;
using VContainer;

namespace Project.GameDomain.Features.Input.Scripts
{
    public class JoystickComponentListener : ComponentListener<JoystickComponent>
    {
        [SerializeField] private SpriteRenderer _baseRenderer;
        [SerializeField] private SpriteRenderer _handleRenderer;

        private ICameraService _cameraService;

        [Inject]
        private void Construct(ICameraService cameraService)
        {
            _cameraService = cameraService;
        }

        public override void UpdateView(in JoystickComponent component)
        {
            bool isActive = component.IsPressed;
            _baseRenderer.enabled = isActive;
            _handleRenderer.enabled = isActive;

            if (!isActive)
            {
                return;
            }

            Vector2 world = _cameraService.ConvertScreenToWorldPosition(
                new Vector2(component.Initial.x, component.Initial.y));
            transform.position = new Vector3(world.x, world.y, 0f);

            float travel = _baseRenderer.sprite.bounds.extents.x - _handleRenderer.sprite.bounds.extents.x;
            _handleRenderer.transform.localPosition =
                new Vector3(component.Axis.x, component.Axis.y, 0f) * travel;
        }
    }
}
