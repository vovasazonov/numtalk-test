using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;

namespace Project.GameDomain.Features.Reaper.Scripts
{
    public sealed class ReaperSystem : UnitySystemBase
    {
        private readonly QueryDescription _reapable = new QueryDescription().WithAll<ReaperComponent>();
        private readonly ForEachWithEntity<ReaperComponent> _tickLifetime;
        private readonly List<Entity> _expired = new();

        private float _deltaTime;

        public ReaperSystem(World world) : base(world)
        {
            _tickLifetime = TickLifetime;
        }

        public override void Update(in SystemState state)
        {
            _deltaTime = state.DeltaTime;
            _expired.Clear();
            World.Query(in _reapable, _tickLifetime);

            for (int index = 0; index < _expired.Count; index++)
            {
                World.Destroy(_expired[index]);
            }
        }

        private void TickLifetime(Entity entity, ref ReaperComponent reaper)
        {
            reaper.TimeRemaining -= _deltaTime;

            if (reaper.TimeRemaining <= 0f)
            {
                _expired.Add(entity);
            }
        }
    }
}
