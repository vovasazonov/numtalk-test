using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Input.Scripts;
using Unity.Mathematics;
using UnityEngine.InputSystem;

namespace Project.GameDomain.Features.GameInput.Scripts
{
    public sealed class GameInputSystem : UnitySystemBase
    {
        private const float _deadzone = 0.0001f;

        private readonly QueryDescription _joysticks = new QueryDescription().WithAll<JoystickComponent>();
        private readonly QueryDescription _presses = new QueryDescription().WithAll<PointerPressComponent>();
        private readonly ForEach<JoystickComponent> _readJoystick;
        private readonly ForEach<PointerPressComponent> _readPress;

        private Entity _moveEntity = Entity.Null;
        private Entity _jumpEntity = Entity.Null;

        private float2 _joystickAxis;
        private bool _wasTapped;

        public GameInputSystem(World world) : base(world)
        {
            _readJoystick = ReadJoystick;
            _readPress = ReadPress;
        }

        public override void Update(in SystemState state)
        {
            _joystickAxis = float2.zero;
            _wasTapped = false;
            World.Query(in _joysticks, _readJoystick);
            World.Query(in _presses, _readPress);

            UpdateMoveInput();
            UpdateJumpInput();
        }

        private void ReadJoystick(ref JoystickComponent joystick)
        {
            if (joystick.IsPressed)
            {
                _joystickAxis = joystick.Axis;
            }
        }

        private void ReadPress(ref PointerPressComponent press)
        {
            if (press.WasTapped)
            {
                _wasTapped = true;
            }
        }

        private void UpdateMoveInput()
        {
            float2 direction = ReadKeyboardMove() + _joystickAxis;

            if (math.lengthsq(direction) > _deadzone)
            {
                direction = math.clamp(direction, -1f, 1f);

                if (World.IsAlive(_moveEntity))
                {
                    World.Set(_moveEntity, new MoveInputComponent { Direction = direction });
                }
                else
                {
                    _moveEntity = World.Create(new MoveInputComponent { Direction = direction });
                }
            }
            else if (World.IsAlive(_moveEntity))
            {
                World.Destroy(_moveEntity);
                _moveEntity = Entity.Null;
            }
        }

        private void UpdateJumpInput()
        {
            Keyboard keyboard = Keyboard.current;
            bool wasJumped = _wasTapped || (keyboard != null && keyboard.spaceKey.wasPressedThisFrame);

            if (wasJumped)
            {
                if (!World.IsAlive(_jumpEntity))
                {
                    _jumpEntity = World.Create(new JumpInputComponent());
                }
            }
            else if (World.IsAlive(_jumpEntity))
            {
                World.Destroy(_jumpEntity);
                _jumpEntity = Entity.Null;
            }
        }

        private static float2 ReadKeyboardMove()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return float2.zero;
            }

            float2 axis = float2.zero;

            if (keyboard.leftArrowKey.isPressed)
            {
                axis.x -= 1f;
            }

            if (keyboard.rightArrowKey.isPressed)
            {
                axis.x += 1f;
            }

            if (keyboard.downArrowKey.isPressed)
            {
                axis.y -= 1f;
            }

            if (keyboard.upArrowKey.isPressed)
            {
                axis.y += 1f;
            }

            return axis;
        }
    }
}
