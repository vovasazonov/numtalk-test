using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.GameInput.Scripts;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.Jump.Scripts;

namespace Project.GameDomain.Features.Player.Scripts
{
    public sealed class PlayerJumpSystem : UnitySystemBase
    {
        private readonly IConfigService _configService;

        private readonly QueryDescription _jumpInputs = new QueryDescription().WithAll<JumpInputComponent>();
        private readonly QueryDescription _players = new QueryDescription()
            .WithAll<PlayerTagComponent>()
            .WithNone<JumpRequestComponent>();

        private readonly ForEach<JumpInputComponent> _readJumpInput;
        private readonly ForEachWithEntity<PlayerTagComponent> _collectRequesters;
        private readonly List<Entity> _requesters = new();

        private bool _isJumpRequested;

        private PlayerConfig _config;

        public PlayerJumpSystem(World world, IConfigService configService) : base(world)
        {
            _configService = configService;
            _readJumpInput = ReadJumpInput;
            _collectRequesters = CollectRequesters;
        }

        public override void Initialize()
        {
            _config = _configService.Get<PlayerConfig>();
        }

        public override void Update(in SystemState state)
        {
            _isJumpRequested = false;
            World.Query(in _jumpInputs, _readJumpInput);

            if (!_isJumpRequested)
            {
                return;
            }

            _requesters.Clear();
            World.Query(in _players, _collectRequesters);

            for (int index = 0; index < _requesters.Count; index++)
            {
                World.Add(_requesters[index], new JumpRequestComponent { Force = _config.JumpForce });
            }
        }

        private void ReadJumpInput(ref JumpInputComponent input)
        {
            _isJumpRequested = true;
        }

        private void CollectRequesters(Entity entity, ref PlayerTagComponent tag)
        {
            _requesters.Add(entity);
        }
    }
}
