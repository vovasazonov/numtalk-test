using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.CameraControl.Scripts
{
    /// <summary>Late presentation uses the same interpolated player pose as the visible shape.</summary>
    public sealed class CameraFollowSystem : UnitySystemBase
    {
        private readonly PlatformerTuningConfig _tuning;
        private readonly CourseCameraPresentation _presentation;
        private readonly QueryDescription _players = new QueryDescription().WithAll<PlayerTagComponent, PlayerMotorComponent, GroundStateComponent, EntityTransformComponent>();
        private readonly ForEach _follow;
        private CameraFollowState _followState;
        private Entity _target;
        private bool _found;
        private float _dt;
        private int _respawnVersion;

        public CameraFollowSystem(World world, PlatformerTuningConfig tuning, CourseCameraPresentation presentation) : base(world)
        {
            _tuning = tuning;
            _presentation = presentation;
            _follow = Follow;
        }

        public override void Update(in SystemState state)
        {
            _found = false;
            _dt = state.DeltaTime;
            World.Query(in _players, _follow);
            if (!_found)
            {
                _followState = default;
                _presentation.Restore();
            }
        }

        private void Follow(Entity entity)
        {
            if (_found) return;
            var motor = World.Get<PlayerMotorComponent>(entity);
            if (!motor.HasSimulationPose) return;
            _found = true;
            if (entity != _target) _followState = default;
            _target = entity;
            var pose = World.Get<EntityTransformComponent>(entity);
            var ground = World.Get<GroundStateComponent>(entity);
            float alpha = Mathf.Clamp01((float)((Time.timeAsDouble - Time.fixedTimeAsDouble) / Time.fixedDeltaTime));
            float3 position = math.lerp(motor.PreviousPosition, pose.Position, alpha);
            // Actual swept displacement includes impulses and platform motion but excludes motion blocked by walls.
            float3 velocity = (pose.Position - motor.PreviousPosition) / Time.fixedDeltaTime;
            if (World.TryGet(entity, out HealthComponent health) && health.RespawnVersion != _respawnVersion)
            {
                _respawnVersion = health.RespawnVersion;
                _followState.BeginRespawn(_tuning.RespawnCameraDuration);
            }
            _followState.Step(position, velocity, ground.IsGrounded, _dt, _tuning);
            _presentation.Apply(_followState.Anchor, _tuning);
        }

        public override void Dispose()
        {
            _presentation.Restore();
            base.Dispose();
        }
    }
}
