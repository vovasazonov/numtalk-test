using Unity.Mathematics;

namespace Project.GameDomain.Features.PlayerInput.Scripts
{
    /// <summary>
    /// Raw per-render-frame input state. Implementations report levels only; <see cref="InputLatchSystem"/> derives
    /// the press and release edges from them, so no source has to track edges of its own.
    /// </summary>
    public interface IPlayerInputSource
    {
        /// <summary>Thumb intent on the unit disc.</summary>
        float2 Move { get; }

        bool JumpHeld { get; }
    }
}
