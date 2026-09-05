using Arch.Core;
using Project.GameDomain.Features.Configs.Scripts;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Course.Scripts
{
    /// <summary>
    /// Turns damage into the run's only failure path: lose a life and resume from the last checkpoint, or, with no
    /// lives left, restart the whole course from its authored state.
    /// </summary>
    public sealed class RespawnSystem : UnitySystemBase
    {
        private readonly CourseSnapshotService _snapshots;
        private readonly PlatformerTuningConfig _tuning;
        private float _dt;

        private readonly QueryDescription _players = new QueryDescription()
            .WithAll<PlayerTagComponent, HealthComponent, CheckpointReferenceComponent, RunStateComponent>();

        private readonly ForEach _resolve;

        public RespawnSystem(World world, CourseSnapshotService snapshots, PlatformerTuningConfig tuning) : base(world)
        {
            _snapshots = snapshots;
            _tuning = tuning;
            _resolve = Resolve;
        }

        public override void Update(in SystemState state)
        {
            _dt = state.DeltaTime;
            if (state.DeltaTime > 0f) World.Query(in _players, _resolve);
        }

        private void Resolve(Entity entity)
        {
            ref var run = ref World.Get<RunStateComponent>(entity);
            if (run.RestartRequested)
            {
                run = new RunStateComponent();
                ref var restarting = ref World.Get<HealthComponent>(entity);
                Restart(entity, ref restarting);
                PlaceAtRespawn(entity);
                return;
            }

            ref var health = ref World.Get<HealthComponent>(entity);
            if (health.Phase == PlayerLifePhase.Dying)
            {
                health.PendingDamage = 0;
                health.FellOutOfCourse = false;
                health.PhaseRemaining = math.max(0f, health.PhaseRemaining - _dt);
                if (health.PhaseRemaining > 0.0001f) return;
                if (health.Lives <= 0)
                {
                    run = new RunStateComponent();
                    Restart(entity, ref health);
                }
                else _snapshots.RestoreCheckpoint();
                PlaceAtRespawn(entity);
                return;
            }
            if (health.Phase == PlayerLifePhase.Respawning && !health.FellOutOfCourse)
            {
                health.PendingDamage = 0;
                health.PhaseRemaining = math.max(0f, health.PhaseRemaining - _dt);
                if (health.PhaseRemaining <= 0.0001f) health.Phase = PlayerLifePhase.Alive;
                return;
            }
            if (health.PendingDamage <= 0) return;

            // Multiple contacts in one tick still cost exactly one life.
            health.PendingDamage = 0;
            health.FellOutOfCourse = false;
            health.Lives--;
            health.Phase = PlayerLifePhase.Dying;
            health.PhaseRemaining = _tuning.DeathDuration;
            ClearMotion(entity);
        }

        private void Restart(Entity entity, ref HealthComponent health)
        {
            health.Lives = health.MaximumLives;
            World.Get<CheckpointReferenceComponent>(entity) = new CheckpointReferenceComponent
            {
                RespawnPosition = World.Get<InitialStateComponent>(entity).Position,
            };

            _snapshots.RestoreRunStart();
        }

        /// <summary>Every channel is cleared, so a respawn never inherits the velocity that caused the death.</summary>
        private void PlaceAtRespawn(Entity entity)
        {
            ref var pose = ref World.Get<EntityTransformComponent>(entity);
            pose.Position = World.Get<CheckpointReferenceComponent>(entity).RespawnPosition;
            pose.Rotation = World.Get<InitialStateComponent>(entity).Rotation;

            ref var health = ref World.Get<HealthComponent>(entity);
            health.Phase = PlayerLifePhase.Respawning;
            health.PhaseRemaining = _tuning.RespawnBlinkDuration;
            health.PendingDamage = 0;
            health.FellOutOfCourse = false;
            health.RespawnVersion++;
            ClearMotion(entity);
        }

        private void ClearMotion(Entity entity)
        {
            World.Get<PlayerMotorComponent>(entity) = new PlayerMotorComponent
            {
                PreviousPosition = World.Get<EntityTransformComponent>(entity).Position,
                HasSimulationPose = true,
            };
            World.Get<ExternalVelocityComponent>(entity) = new ExternalVelocityComponent();
            World.Get<PlatformRiderComponent>(entity) = new PlatformRiderComponent();
            World.Get<JumpStateComponent>(entity) = new JumpStateComponent();
            World.Get<GroundStateComponent>(entity) = new GroundStateComponent { GroundNormal = math.up() };
        }
    }
}
