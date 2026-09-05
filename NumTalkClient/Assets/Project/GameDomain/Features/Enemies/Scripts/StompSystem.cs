using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Enemies.Scripts
{
    /// <summary>
    /// Decides stomp against hurt from the segment the player actually travelled this fixed step, not from a
    /// contact callback. The player's capsule bottom where the step began, compared against the enemy's top, is
    /// what separates the two: it stays correct at terminal fall speed on a long frame, where the resulting pose
    /// is already past the enemy and the post-move contact normal says nothing useful.
    /// </summary>
    public sealed class StompSystem : UnitySystemBase
    {
        /// <summary>How far below an enemy's top the capsule may start and still count as coming down on it.</summary>
        private const float TopContactTolerance = 0.05f;

        /// <summary>Clearance left above the enemy after a stomp, so the bounce does not start inside it.</summary>
        private const float StompClearance = 0.02f;

        /// <summary>Sweep length past the travelled segment, covering the gap the controller stops short at.</summary>
        private const float ContactSkin = 0.12f;

        private readonly CharacterMotionService _motion;
        private readonly PlatformerTuningConfig _tuning;

        private readonly QueryDescription _players = new QueryDescription()
            .WithAll<PlayerTagComponent, PlayerMotorComponent, CharacterBodyComponent, HealthComponent>();

        private readonly ForEach _resolve;
        private readonly List<Entity> _defeated = new();

        public StompSystem(World world, CharacterMotionService motion, PlatformerTuningConfig tuning) : base(world)
        {
            _motion = motion;
            _tuning = tuning;
            _resolve = Resolve;
        }

        public override void Update(in SystemState state)
        {
            if (state.DeltaTime <= 0f) return;

            World.Query(in _players, _resolve);

            // Applied after the query, so the enemy's view is released outside the walk over the player archetype.
            for (int index = 0; index < _defeated.Count; index++)
            {
                Entity enemy = _defeated[index];
                if (World.IsAlive(enemy) && World.Has<ViewComponent>(enemy)) World.Remove<ViewComponent>(enemy);
            }

            _defeated.Clear();
        }

        private void Resolve(Entity entity)
        {
            if (!_motion.IsReady(entity)) return;

            ref var motor = ref World.Get<PlayerMotorComponent>(entity);
            if (!motor.HasSimulationPose) return;

            ref var pose = ref World.Get<EntityTransformComponent>(entity);
            ref var external = ref World.Get<ExternalVelocityComponent>(entity);
            var jump = World.Get<JumpStateComponent>(entity);

            float3 from = motor.PreviousPosition;
            float bottomAtStart = from.y + _motion.CapsuleBottomOffset(entity);
            // Still descending covers the blocked step too: the enemy stops the fall, but the motor keeps pulling down.
            bool descending = motor.Velocity.y < 0f || pose.Position.y < from.y;

            IReadOnlyList<CharacterSweepHit> hits =
                _motion.Sweep(entity, from, pose.Position, _tuning.EnemyContactMask, ContactSkin);
            for (int index = 0; index < hits.Count; index++)
            {
                CharacterSweepHit hit = hits[index];
                if (!World.IsAlive(hit.Other)) continue;
                if (!World.TryGet(hit.Other, out StompTargetComponent stomp) || stomp.IsDefeated) continue;

                bool fromAbove = bottomAtStart >= hit.TopY - TopContactTolerance;
                if (descending && fromAbove)
                {
                    Defeat(hit, ref motor, ref pose, entity, jump.IsHeld);
                    continue;
                }

                Hurt(hit, from, ref external);
                World.Get<HealthComponent>(entity).PendingDamage++;
            }
        }

        private void Defeat(CharacterSweepHit hit, ref PlayerMotorComponent motor,
            ref EntityTransformComponent pose, Entity player, bool jumpHeld)
        {
            World.Get<StompTargetComponent>(hit.Other).IsDefeated = true;
            _defeated.Add(hit.Other);

            // Land on the enemy's top rather than wherever the swept step ended, which at terminal speed is below it.
            pose.Position.y = hit.TopY + StompClearance - _motion.CapsuleBottomOffset(player);
            motor.Velocity.y = jumpHeld ? _tuning.HeldJumpStompBounceSpeed : _tuning.StompBounceSpeed;
        }

        /// <summary>A side or underside contact pushes the player away from the enemy, horizontally and readably.</summary>
        private void Hurt(CharacterSweepHit hit, float3 from, ref ExternalVelocityComponent external)
        {
            float3 away = math.normalizesafe(new float3(from.x - hit.Point.x, 0f, from.z - hit.Point.z),
                new float3(hit.Normal.x, 0f, hit.Normal.z));
            external.Velocity = math.normalizesafe(away) * _tuning.KnockbackSpeed;
        }
    }
}
