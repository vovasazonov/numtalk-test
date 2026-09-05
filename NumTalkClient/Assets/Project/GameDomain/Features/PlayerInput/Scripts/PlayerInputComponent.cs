using Unity.Mathematics;

namespace Project.GameDomain.Features.PlayerInput.Scripts
{
    /// <summary>
    /// Player intent sampled on the render frame and read by the fixed-step simulation. <see cref="Move"/> and
    /// <see cref="JumpHeld"/> are levels that always describe the latest sample; <see cref="JumpPressed"/> and
    /// <see cref="JumpReleased"/> are latched edges, so a tap shorter than one fixed tick still reaches the motor.
    /// The edges are cleared by <see cref="InputLatchResetSystem"/> at the end of the tick that consumed them.
    /// </summary>
    public struct PlayerInputComponent
    {
        /// <summary>Thumb intent on the unit disc, interpreted camera-relative by the motor.</summary>
        public float2 Move;

        public bool JumpHeld;
        public bool JumpPressed;
        public bool JumpReleased;
    }
}
