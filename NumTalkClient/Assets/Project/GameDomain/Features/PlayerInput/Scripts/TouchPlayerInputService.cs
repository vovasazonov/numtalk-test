using Project.GameDomain.Features.Configs.Scripts;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#if UNITY_EDITOR
using UnityEngine.InputSystem.EnhancedTouch;
#endif

namespace Project.GameDomain.Features.PlayerInput.Scripts
{
    /// <summary>
    /// Two-thumb touch controls. The first unclaimed touch that lands in the left region anchors a floating stick
    /// and its drag becomes the move vector; the first in the right region holds jump. Each thumb owns its touch id
    /// until that finger lifts, so the two operate concurrently and neither can steal the other's finger.
    /// </summary>
    public sealed class TouchPlayerInputService : IPlayerInputSource, System.IDisposable
    {
        private const int NoTouch = 0;
        private const float FallbackScreenDpi = 160f;

        private readonly PlatformerTuningConfig _tuning;

#if UNITY_EDITOR
        private readonly bool _ownsTouchSimulation;
#endif

        private int _stickTouchId = NoTouch;
        private int _jumpTouchId = NoTouch;
        private Vector2 _stickCenter;

        public float2 Move { get; private set; }

        public bool JumpHeld { get; private set; }

        /// <summary>Screen-space stick state, for the on-screen control view. Only valid while <see cref="StickIsDown"/>.</summary>
        public bool StickIsDown => _stickTouchId != NoTouch;

        public Vector2 StickCenter => _stickCenter;

        /// <summary>Thumb position clamped to the stick radius, so the drawn knob stops at the ring.</summary>
        public Vector2 StickKnob { get; private set; }

        public Vector2 JumpPosition { get; private set; }

        public float StickRadiusPixels => _tuning.StickMaximumRadiusInches * ScreenDpi;

        private static float ScreenDpi => Screen.dpi > 0f ? Screen.dpi : FallbackScreenDpi;

        public TouchPlayerInputService(PlatformerTuningConfig tuning)
        {
            _tuning = tuning;

#if UNITY_EDITOR
            // With no touchscreen - a plain Game view - let the mouse drive one simulated finger so the controls
            // can still be exercised. When something already provides one, such as the Device Simulator, leave it
            // alone: a second touchscreen would win Touchscreen.current and report positions in the wrong space.
            _ownsTouchSimulation = Touchscreen.current == null;
            if (_ownsTouchSimulation)
            {
                TouchSimulation.Enable();
            }
#endif
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            if (_ownsTouchSimulation)
            {
                TouchSimulation.Disable();
            }
#endif
        }

        public void Sample()
        {
            Move = float2.zero;
            JumpHeld = false;

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                _stickTouchId = NoTouch;
                _jumpTouchId = NoTouch;
                return;
            }

            float jumpRegionMinimumX = Screen.width * _tuning.JumpRegionScreenFraction;
            bool stickIsDown = false;
            bool jumpIsDown = false;

            foreach (TouchControl touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                {
                    continue;
                }

                int touchId = touch.touchId.ReadValue();
                Vector2 position = touch.position.ReadValue();

                if (touchId == _stickTouchId)
                {
                    stickIsDown = true;
                    Move = Deflection(position);
                }
                else if (touchId == _jumpTouchId)
                {
                    jumpIsDown = true;
                    JumpPosition = position;
                }
                else if (position.x < jumpRegionMinimumX)
                {
                    if (_stickTouchId == NoTouch)
                    {
                        _stickTouchId = touchId;
                        _stickCenter = position;
                        StickKnob = position;
                        stickIsDown = true;
                    }
                }
                else if (_jumpTouchId == NoTouch)
                {
                    _jumpTouchId = touchId;
                    JumpPosition = position;
                    jumpIsDown = true;
                }
            }

            if (!stickIsDown)
            {
                _stickTouchId = NoTouch;
            }

            if (!jumpIsDown)
            {
                _jumpTouchId = NoTouch;
            }

            JumpHeld = jumpIsDown;
        }

        /// <summary>
        /// Drag from the anchored centre, mapped onto the unit disc. Radii are configured in inches and converted
        /// with the screen DPI, so the stick covers the same physical thumb travel on every device.
        /// </summary>
        private float2 Deflection(Vector2 position)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : FallbackScreenDpi;
            float deadZone = _tuning.StickDeadZoneInches * dpi;
            float maximumRadius = _tuning.StickMaximumRadiusInches * dpi;

            Vector2 drag = position - _stickCenter;
            float distance = drag.magnitude;
            StickKnob = _stickCenter + Vector2.ClampMagnitude(drag, maximumRadius);

            if (distance <= deadZone || maximumRadius <= deadZone)
            {
                return float2.zero;
            }

            float magnitude = math.min((distance - deadZone) / (maximumRadius - deadZone), 1f);
            return new float2(drag.x, drag.y) / distance * magnitude;
        }
    }
}
