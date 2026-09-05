using Unity.Mathematics;

namespace Project.GameDomain.Features.PlayerInput.Scripts
{
    /// <summary>
    /// Merges every input source so touch and keyboard are live at the same time. Whichever source is actually
    /// deflected wins the move vector, and jump is held if any source holds it, so neither device fights the other.
    /// </summary>
    public sealed class CompositePlayerInputSource : IPlayerInputSource
    {
        private readonly IPlayerInputSource[] _sources;

        public float2 Move { get; private set; }

        public bool JumpHeld { get; private set; }

        public CompositePlayerInputSource(params IPlayerInputSource[] sources)
        {
            _sources = sources;
        }

        public void Sample()
        {
            Move = float2.zero;
            JumpHeld = false;

            foreach (IPlayerInputSource source in _sources)
            {
                source.Sample();

                if (math.lengthsq(source.Move) > math.lengthsq(Move))
                {
                    Move = source.Move;
                }

                JumpHeld |= source.JumpHeld;
            }
        }
    }
}
