using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Checkpoints.Scripts;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Hazards.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Pickup.Scripts;
using Project.GameDomain.Features.Player.Scripts;

namespace Project.GameDomain.Features.Course.Scripts
{
    /// <summary>
    /// Reads the trigger volumes the player is standing in every fixed step, rather than waiting for a collision
    /// callback: checkpoints light up, coins are collected, and the kill plane costs a life.
    /// </summary>
    public sealed class CourseTriggerSystem : UnitySystemBase
    {
        private readonly CharacterMotionService _motion;
        private readonly CourseSnapshotService _snapshots;
        private readonly PlatformerTuningConfig _tuning;

        private readonly QueryDescription _players = new QueryDescription()
            .WithAll<PlayerTagComponent, HealthComponent, CharacterBodyComponent>();

        private readonly ForEach _read;
        private readonly List<Entity> _collected = new();

        public CourseTriggerSystem(World world, CharacterMotionService motion, CourseSnapshotService snapshots,
            PlatformerTuningConfig tuning) : base(world)
        {
            _motion = motion;
            _snapshots = snapshots;
            _tuning = tuning;
            _read = Read;
        }

        public override void Update(in SystemState state)
        {
            if (state.DeltaTime <= 0f) return;

            _snapshots.CaptureRunStart();
            World.Query(in _players, _read);

            // Outside the query, because hiding a collected coin is a structural change.
            for (int index = 0; index < _collected.Count; index++)
            {
                Entity coin = _collected[index];
                if (World.IsAlive(coin) && World.Has<ViewComponent>(coin)) World.Remove<ViewComponent>(coin);
            }

            _collected.Clear();
        }

        private void Read(Entity entity)
        {
            if (!_motion.IsReady(entity)) return;

            IReadOnlyList<Entity> overlapped = _motion.Overlap(entity, _tuning.TriggerContactMask);
            for (int index = 0; index < overlapped.Count; index++)
            {
                Entity other = overlapped[index];
                if (!World.IsAlive(other)) continue;

                if (World.Has<KillZoneComponent>(other)) World.Get<HealthComponent>(entity).PendingDamage++;
                if (World.Has<PickupComponent>(other)) Collect(other);
                if (World.Has<CheckpointComponent>(other)) Activate(entity, other);
            }
        }

        private void Collect(Entity coin)
        {
            ref var pickup = ref World.Get<PickupComponent>(coin);
            if (pickup.IsCollected) return;

            pickup.IsCollected = true;
            _collected.Add(coin);
        }

        /// <summary>
        /// A checkpoint only ever moves the run forward, and the snapshot is taken after it is lit, so restoring it
        /// restores a world in which this checkpoint is already the current one.
        /// </summary>
        private void Activate(Entity player, Entity checkpoint)
        {
            ref var marker = ref World.Get<CheckpointComponent>(checkpoint);
            ref var reference = ref World.Get<CheckpointReferenceComponent>(player);
            if (marker.Id <= reference.CheckpointId) return;

            marker.IsActivated = true;
            reference.CheckpointId = marker.Id;
            reference.RespawnPosition = marker.RespawnPosition;
            _snapshots.CaptureCheckpoint();
        }
    }
}
