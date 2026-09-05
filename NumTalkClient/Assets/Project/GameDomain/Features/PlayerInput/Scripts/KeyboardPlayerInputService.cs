using Unity.Mathematics;
using UnityEngine.InputSystem;

namespace Project.GameDomain.Features.PlayerInput.Scripts
{
    /// <summary>
    /// WASD and space, for playing the course at a desk. The device target is still two thumbs; this exists so the
    /// course can be exercised without a build. Reports levels only, like every other source.
    /// </summary>
    public sealed class KeyboardPlayerInputService : IPlayerInputSource
    {
        public float2 Move { get; private set; }

        public bool JumpHeld { get; private set; }

        public void Sample()
        {
            Move = float2.zero;
            JumpHeld = false;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var move = new float2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));

            // Clamped rather than normalised, so a single key still reads as full deflection on one axis.
            Move = move / math.max(1f, math.length(move));
            JumpHeld = keyboard.spaceKey.isPressed;
        }
    }
}
