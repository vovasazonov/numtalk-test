using Unity.Mathematics;

namespace Project.GameDomain.Features.PlayerInput.Scripts
{
    /// <summary>
    /// Neutral input, so the simulation schedule runs before the two-thumb touch source exists. Replaced by the
    /// floating stick and jump region in A5.
    /// </summary>
    public sealed class NullPlayerInputSource : IPlayerInputSource
    {
        public float2 Move => float2.zero;

        public bool JumpHeld => false;
    }
}
