using Arch.Core;
using Arch.Unity.Toolkit;
using Unity.Mathematics;

namespace Project.GameDomain.Features.PlayerInput.Scripts
{
    /// <summary>
    /// Samples the input source once per render frame and latches its edges into <see cref="PlayerInputComponent"/>.
    /// Runs on the render frame rather than the fixed tick so a short tap is never dropped at a low frame rate.
    /// </summary>
    public sealed class InputLatchSystem : UnitySystemBase
    {
        private readonly IPlayerInputSource _source;
        private readonly QueryDescription _players = new QueryDescription().WithAll<PlayerInputComponent>();
        private readonly ForEach _latch;

        private float2 _sampledMove;
        private bool _sampledJumpHeld;

        public InputLatchSystem(World world, IPlayerInputSource source) : base(world)
        {
            _source = source;
            _latch = Latch;
        }

        public override void Update(in SystemState state)
        {
            _source.Sample();

            _sampledMove = _source.Move;
            _sampledJumpHeld = _source.JumpHeld;

            World.Query(in _players, _latch);
        }

        private void Latch(Entity entity)
        {
            ref PlayerInputComponent input = ref World.Get<PlayerInputComponent>(entity);

            input.Move = _sampledMove;
            input.JumpPressed |= _sampledJumpHeld && !input.JumpHeld;
            input.JumpReleased |= !_sampledJumpHeld && input.JumpHeld;
            input.JumpHeld = _sampledJumpHeld;
        }
    }
}
