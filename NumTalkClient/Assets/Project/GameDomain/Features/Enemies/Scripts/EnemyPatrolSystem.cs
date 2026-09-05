using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Enemies.Scripts
{
    /// <summary>
    /// Walks patrol enemies along their authored route in fixed time, pausing at each end. The pose is written to
    /// ECS, so the enemy's collider follows through the same listener every other course object uses.
    /// </summary>
    public sealed class EnemyPatrolSystem : UnitySystemBase
    {
        private readonly QueryDescription _patrols = new QueryDescription()
            .WithAll<EnemyComponent, PatrolComponent, EntityTransformComponent>();

        private readonly ForEach _step;
        private float _dt;

        public EnemyPatrolSystem(World world) : base(world) => _step = Step;

        public override void Update(in SystemState state)
        {
            _dt = state.DeltaTime;
            if (_dt > 0f) World.Query(in _patrols, _step);
        }

        private void Step(Entity entity)
        {
            if (World.TryGet(entity, out StompTargetComponent stomp) && stomp.IsDefeated) return;

            ref var patrol = ref World.Get<PatrolComponent>(entity);
            ref var pose = ref World.Get<EntityTransformComponent>(entity);

            if (patrol.WaitTimer > 0f)
            {
                patrol.WaitTimer = math.max(0f, patrol.WaitTimer - _dt);
                return;
            }

            float3 target = patrol.IsForward ? patrol.EndPosition : patrol.StartPosition;
            float3 offset = target - pose.Position;
            float remaining = math.length(offset);
            float travel = patrol.Speed * _dt;

            if (remaining <= travel)
            {
                pose.Position = target;
                patrol.IsForward = !patrol.IsForward;
                patrol.WaitTimer = patrol.WaitTime;
                return;
            }

            pose.Position += offset / remaining * travel;
        }
    }
}
