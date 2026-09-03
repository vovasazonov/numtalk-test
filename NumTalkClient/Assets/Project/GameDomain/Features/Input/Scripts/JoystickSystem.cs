using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.GameDomain.Features.Input.Scripts
{
    public sealed class JoystickSystem : UnitySystemBase
    {
        private const float _maxRadiusPixels = 150f;

        private readonly QueryDescription _inputs = new QueryDescription().WithAll<JoystickComponent>();
        private readonly ForEach<JoystickComponent> _capture;

        private float2 _position;
        private bool _wasPressed;
        private bool _isPressed;

        public JoystickSystem(World world) : base(world)
        {
            _capture = Capture;
        }

        public override void Initialize()
        {
            World.Create(new ViewComponent(), new JoystickComponent { IsDynamic = true });
        }

        public override void Update(in SystemState state)
        {
            Pointer pointer = Pointer.current;
            _wasPressed = pointer != null && pointer.press.wasPressedThisFrame;
            _isPressed = pointer != null && pointer.press.isPressed;
            _position = pointer != null ? ToFloat2(pointer.position.ReadValue()) : float2.zero;

            World.Query(in _inputs, _capture);
        }

        private void Capture(ref JoystickComponent input)
        {
            if (_wasPressed)
            {
                input.IsPressed = true;
                input.Initial = _position;
                input.Axis = float2.zero;
            }
            else if (_isPressed)
            {
                input.IsPressed = true;
                float2 delta = _position - input.Initial;
                float distance = math.length(delta);
                float2 direction = math.normalizesafe(delta);

                if (input.IsDynamic && distance > _maxRadiusPixels)
                {
                    input.Initial = _position - direction * _maxRadiusPixels;
                    distance = _maxRadiusPixels;
                }

                input.Axis = direction * math.saturate(distance / _maxRadiusPixels);
            }
            else
            {
                input.IsPressed = false;
                input.Initial = float2.zero;
                input.Axis = float2.zero;
            }
        }

        private static float2 ToFloat2(Vector2 value)
        {
            return new float2(value.x, value.y);
        }
    }
}
