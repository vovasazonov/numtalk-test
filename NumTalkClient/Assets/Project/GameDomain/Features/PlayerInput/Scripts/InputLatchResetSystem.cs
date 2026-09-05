using Arch.Core;
using Arch.Unity.Toolkit;

namespace Project.GameDomain.Features.PlayerInput.Scripts
{
    /// <summary>
    /// Clears the latched jump edges at the end of the fixed tick. Registered last in the fixed-step schedule, so
    /// every simulation system in that tick sees the same edge exactly once.
    /// </summary>
    public sealed class InputLatchResetSystem : UnitySystemBase
    {
        private readonly QueryDescription _players = new QueryDescription().WithAll<PlayerInputComponent>();
        private readonly ForEach _clearEdges;

        public InputLatchResetSystem(World world) : base(world)
        {
            _clearEdges = ClearEdges;
        }

        public override void Update(in SystemState state)
        {
            World.Query(in _players, _clearEdges);
        }

        private void ClearEdges(Entity entity)
        {
            ref PlayerInputComponent input = ref World.Get<PlayerInputComponent>(entity);

            input.JumpPressed = false;
            input.JumpReleased = false;
        }
    }
}
