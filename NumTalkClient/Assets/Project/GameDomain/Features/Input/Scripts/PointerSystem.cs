using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Project.GameDomain.Features.Input.Scripts
{
    public sealed class PointerSystem : UnitySystemBase
    {
        private const float _tapMaxDistancePixels = 20f;
        private const float _tapMaxDurationSeconds = 0.3f;

        private readonly QueryDescription _inputs = new QueryDescription().WithAll<PointerPressComponent>();
        private readonly ForEach<PointerPressComponent> _capture;
        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();

        private bool _wasClicked;
        private bool _wasTapped;
        private bool _canPressBecomeTap;
        private float2 _pressPosition;
        private float _pressDuration;

        public PointerSystem(World world) : base(world)
        {
            _capture = Capture;
        }

        public override void Initialize()
        {
            World.Create(new PointerPressComponent());
        }

        public override void Update(in SystemState state)
        {
            Pointer pointer = Pointer.current;
            _wasClicked = pointer != null && pointer.press.wasPressedThisFrame;
            _wasTapped = false;

            if (pointer != null)
            {
                TrackTap(pointer, state.DeltaTime);
            }

            World.Query(in _inputs, _capture);
        }

        private void TrackTap(Pointer pointer, float deltaTime)
        {
            float2 position = ToFloat2(pointer.position.ReadValue());

            if (pointer.press.wasPressedThisFrame)
            {
                _canPressBecomeTap = !IsOverUi(position);
                _pressPosition = position;
                _pressDuration = 0f;
            }

            if (!_canPressBecomeTap)
            {
                return;
            }

            _pressDuration += deltaTime;

            bool isTapSized = _pressDuration <= _tapMaxDurationSeconds
                && math.distance(position, _pressPosition) <= _tapMaxDistancePixels;

            if (pointer.press.wasReleasedThisFrame)
            {
                _wasTapped = isTapSized;
                _canPressBecomeTap = false;
            }
            else if (!isTapSized)
            {
                _canPressBecomeTap = false;
            }
        }

        private bool IsOverUi(float2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null)
            {
                return false;
            }

            PointerEventData pointerEventData = new PointerEventData(eventSystem)
            {
                position = new Vector2(screenPosition.x, screenPosition.y)
            };

            _uiRaycastResults.Clear();
            eventSystem.RaycastAll(pointerEventData, _uiRaycastResults);

            return _uiRaycastResults.Count > 0;
        }

        private void Capture(ref PointerPressComponent input)
        {
            input.WasPressed = _wasClicked;
            input.WasTapped = _wasTapped;
        }

        private static float2 ToFloat2(Vector2 value)
        {
            return new float2(value.x, value.y);
        }
    }
}
