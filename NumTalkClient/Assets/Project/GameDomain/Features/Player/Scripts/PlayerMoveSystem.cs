using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.GameInput.Scripts;
using Project.GameDomain.Features.Location.Scripts;
using Project.GameDomain.Features.Movement.Scripts;
using Project.GameDomain.Features.Position.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Player.Scripts
{
    public sealed class PlayerMoveSystem : UnitySystemBase
    {
        private readonly IConfigService _configService;

        private readonly QueryDescription _inputs = new QueryDescription().WithAll<MoveInputComponent>();
        private readonly QueryDescription _locations = new QueryDescription().WithAll<LocationComponent>();
        private readonly QueryDescription _players =
            new QueryDescription().WithAll<PlayerTagComponent, MovementComponent, PositionComponent>();

        private readonly ForEach<MoveInputComponent> _readInput;
        private readonly ForEach<LocationComponent> _readMovableHeight;
        private readonly ForEach<PlayerTagComponent, MovementComponent, PositionComponent> _move;

        private float2 _axis;
        private bool _isPressed;
        private float _movableHeight;
        private float _deltaTime;

        private PlayerConfig _config;

        public PlayerMoveSystem(World world, IConfigService configService) : base(world)
        {
            _configService = configService;
            _readInput = ReadInput;
            _readMovableHeight = ReadMovableHeight;
            _move = Move;
        }

        public override void Initialize()
        {
            _config = _configService.Get<PlayerConfig>();
        }

        public override void Update(in SystemState state)
        {
            _axis = float2.zero;
            _isPressed = false;
            _movableHeight = 0f;
            _deltaTime = state.DeltaTime;
            World.Query(in _inputs, _readInput);
            World.Query(in _locations, _readMovableHeight);
            World.Query(in _players, _move);
        }

        private void ReadInput(ref MoveInputComponent input)
        {
            _axis = input.Direction;
            _isPressed = true;
        }

        private void ReadMovableHeight(ref LocationComponent location)
        {
            _movableHeight = location.MovableHeight;
        }

        private void Move(ref PlayerTagComponent tag, ref MovementComponent movement, ref PositionComponent position)
        {
            movement.Velocity.x = math.max(_config.BaseSpeed + _axis.x * _config.HorizontalBoost, _config.MinSpeed);

            movement.Velocity.y = _isPressed
                ? _axis.y * _config.VerticalSpeed
                : math.clamp(-position.Position.y * _config.ReturnRate, -_config.BaseSpeed, _config.BaseSpeed);

            ClampToMovableArea(ref movement, ref position);
        }

        private void ClampToMovableArea(ref MovementComponent movement, ref PositionComponent position)
        {
            if (_movableHeight <= 0f || _deltaTime <= 0f)
            {
                return;
            }

            float halfHeight = _movableHeight * 0.5f;
            float nextY = position.Position.y + movement.Velocity.y * _deltaTime;

            if (nextY > halfHeight)
            {
                movement.Velocity.y = (halfHeight - position.Position.y) / _deltaTime;
            }
            else if (nextY < -halfHeight)
            {
                movement.Velocity.y = (-halfHeight - position.Position.y) / _deltaTime;
            }
        }
    }
}
