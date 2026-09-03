using Arch.Core;
using Arch.Unity.Toolkit;
using Project.CoreDomain.Camera.Scripts;
using Project.GameDomain.Features.Position.Scripts;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.Player.Scripts
{
    public sealed class CameraFollowPlayerSystem : UnitySystemBase
    {
        private const float _smoothing = 5f;
        private readonly float3 _offset = new(0.5f, 0, 0);

        private readonly ICameraService _cameraService;

        private readonly QueryDescription _targets =
            new QueryDescription().WithAll<PlayerTagComponent, PositionComponent>();

        private readonly ForEach<PlayerTagComponent, PositionComponent> _follow;

        private float _deltaTime;

        public CameraFollowPlayerSystem(World world, ICameraService cameraService) : base(world)
        {
            _cameraService = cameraService;
            _follow = Follow;
        }

        public override void Update(in SystemState state)
        {
            _deltaTime = state.DeltaTime;
            World.Query(in _targets, _follow);
        }

        private void Follow(ref PlayerTagComponent follow, ref PositionComponent position)
        {
            Transform cameraTransform = _cameraService.Camera.UnityCamera.transform;
            Vector3 current = cameraTransform.position;

            float targetX = position.Position.x + _offset.x;
            float x = _smoothing > 0f
                ? Mathf.Lerp(current.x, targetX, 1f - Mathf.Exp(-_smoothing * _deltaTime))
                : targetX;

            cameraTransform.position = new Vector3(x, current.y, current.z);
        }
    }
}