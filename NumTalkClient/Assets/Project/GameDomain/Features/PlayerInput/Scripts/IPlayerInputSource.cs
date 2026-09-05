using Unity.Mathematics;

namespace Project.GameDomain.Features.PlayerInput.Scripts
{
    /// <summary>
    /// Raw per-render-frame input state. Implementations report levels only; <see cref="InputLatchSystem"/> derives
    /// the press and release edges from them, so no source has to track edges of its own.
    /// </summary>
    public interface IPlayerInputSource
    {
        /// <summary>
        /// Refreshes the levels from the device. Called by <see cref="InputLatchSystem"/> once per render frame,
        /// immediately before they are read, so the sampling point is explicit rather than left to script order.
        /// </summary>
        void Sample();

        /// <summary>Thumb intent on the unit disc.</summary>
        float2 Move { get; }

        bool JumpHeld { get; }
    }
}
