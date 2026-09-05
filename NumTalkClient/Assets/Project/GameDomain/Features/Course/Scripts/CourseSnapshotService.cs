using Arch.Core;
using Project.GameDomain.Features.Checkpoints.Scripts;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Enemies.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Pickup.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Course.Scripts
{
    /// <summary>
    /// Captures and restores the mutable course state a retry has to be fair about: crate pose, platform phases,
    /// crumble state, enemy life and cooldowns, and which checkpoints are already lit.
    /// </summary>
    /// <remarks>
    /// Collected pickups are captured but only ever restored by a full restart. A respawn deliberately keeps them
    /// collected, so a retry cannot pay the same coin twice, which is why the restore takes that as a flag.
    /// </remarks>
    public sealed class CourseSnapshotService
    {
        private readonly World _world;
        private readonly RigidBodyService _bodies;
        private readonly ProjectilePool _projectiles;

        private readonly CourseSnapshot _runStart = new();
        private readonly CourseSnapshot _checkpoint = new();
        private bool _hasRunStart;

        // Everything with an authored pose except the player, who is placed at a checkpoint, and the projectiles,
        // which are pooled transients rather than course state.
        private readonly QueryDescription _posed = new QueryDescription()
            .WithAll<EntityTransformComponent>()
            .WithNone<PlayerTagComponent, ProjectileComponent>();

        private readonly QueryDescription _platforms = new QueryDescription().WithAll<PlatformMotionComponent>();
        private readonly QueryDescription _crumbles = new QueryDescription().WithAll<CrumbleStateComponent>();
        private readonly QueryDescription _freezes = new QueryDescription().WithAll<FlashFreezeComponent>();
        private readonly QueryDescription _patrols = new QueryDescription().WithAll<PatrolComponent>();
        private readonly QueryDescription _shooters = new QueryDescription().WithAll<ShooterComponent>();
        private readonly QueryDescription _stompTargets = new QueryDescription().WithAll<StompTargetComponent>();
        private readonly QueryDescription _checkpoints = new QueryDescription().WithAll<CheckpointComponent>();
        private readonly QueryDescription _pickups = new QueryDescription().WithAll<PickupComponent>();

        private readonly ForEach _capturePose;
        private readonly ForEach _capturePlatform;
        private readonly ForEach _captureCrumble;
        private readonly ForEach _captureFreeze;
        private readonly ForEach _capturePatrol;
        private readonly ForEach _captureShooter;
        private readonly ForEach _captureStompTarget;
        private readonly ForEach _captureCheckpoint;
        private readonly ForEach _capturePickup;

        private CourseSnapshot _target;

        public CourseSnapshotService(World world, RigidBodyService bodies, ProjectilePool projectiles)
        {
            _world = world;
            _bodies = bodies;
            _projectiles = projectiles;

            _capturePose = CapturePose;
            _capturePlatform = entity => _target.Platforms.Add((entity, _world.Get<PlatformMotionComponent>(entity)));
            _captureCrumble = entity => _target.Crumbles.Add((entity, _world.Get<CrumbleStateComponent>(entity)));
            _captureFreeze = entity => _target.Freezes.Add((entity, _world.Get<FlashFreezeComponent>(entity)));
            _capturePatrol = entity => _target.Patrols.Add((entity, _world.Get<PatrolComponent>(entity)));
            _captureShooter = entity => _target.Shooters.Add((entity, _world.Get<ShooterComponent>(entity)));
            _captureStompTarget = entity => _target.StompTargets.Add((entity, _world.Get<StompTargetComponent>(entity)));
            _captureCheckpoint = entity => _target.Checkpoints.Add((entity, _world.Get<CheckpointComponent>(entity)));
            _capturePickup = entity => _target.Pickups.Add((entity,
                _world.Get<PickupComponent>(entity), _world.Has<ViewComponent>(entity)));
        }

        /// <summary>Taken once, before anything has moved, so a zero-life restart has an authored state to return to.</summary>
        public void CaptureRunStart()
        {
            if (_hasRunStart) return;

            Capture(_runStart);
            Capture(_checkpoint);
            _hasRunStart = true;
        }

        public void CaptureCheckpoint() => Capture(_checkpoint);

        public void RestoreCheckpoint() => Restore(_checkpoint, restorePickups: false);

        public void RestoreRunStart() => Restore(_runStart, restorePickups: true);

        private void Capture(CourseSnapshot snapshot)
        {
            snapshot.Clear();
            _target = snapshot;

            _world.Query(in _posed, _capturePose);
            _world.Query(in _platforms, _capturePlatform);
            _world.Query(in _crumbles, _captureCrumble);
            _world.Query(in _freezes, _captureFreeze);
            _world.Query(in _patrols, _capturePatrol);
            _world.Query(in _shooters, _captureShooter);
            _world.Query(in _stompTargets, _captureStompTarget);
            _world.Query(in _checkpoints, _captureCheckpoint);
            _world.Query(in _pickups, _capturePickup);
        }

        private void CapturePose(Entity entity)
        {
            _target.Poses.Add((entity, _world.Get<EntityTransformComponent>(entity), _world.Has<ViewComponent>(entity)));
            if (!_bodies.IsReady(entity)) return;

            _bodies.Read(entity, out float3 position, out quaternion rotation, out _);
            _target.Bodies.Add((entity, position, rotation));
        }

        private void Restore(CourseSnapshot snapshot, bool restorePickups)
        {
            // Transients first: a projectile in flight belongs to the attempt that just ended.
            _projectiles.ReturnAll();

            foreach ((Entity entity, EntityTransformComponent pose, bool hasView) in snapshot.Poses)
            {
                if (!_world.IsAlive(entity)) continue;
                _world.Get<EntityTransformComponent>(entity) = pose;
                SetViewVisible(entity, hasView);
            }

            foreach ((Entity entity, float3 position, quaternion rotation) in snapshot.Bodies)
            {
                if (_world.IsAlive(entity) && _bodies.IsReady(entity)) _bodies.Teleport(entity, position, rotation);
            }

            Write(snapshot.Platforms);
            Write(snapshot.Crumbles);
            Write(snapshot.Freezes);
            Write(snapshot.Patrols);
            Write(snapshot.Shooters);
            Write(snapshot.StompTargets);
            Write(snapshot.Checkpoints);

            if (!restorePickups) return;

            foreach ((Entity entity, PickupComponent value, bool hasView) in snapshot.Pickups)
            {
                if (!_world.IsAlive(entity)) continue;
                _world.Get<PickupComponent>(entity) = value;
                SetViewVisible(entity, hasView);
            }
        }

        private void Write<T>(System.Collections.Generic.List<(Entity Entity, T Value)> values)
        {
            foreach ((Entity entity, T value) in values)
            {
                if (_world.IsAlive(entity)) _world.Get<T>(entity) = value;
            }
        }

        /// <summary>A defeated enemy or a collected coin is hidden by losing its view; restoring gives it back.</summary>
        private void SetViewVisible(Entity entity, bool visible)
        {
            bool hasView = _world.Has<ViewComponent>(entity);
            if (visible && !hasView) _world.Add(entity, new ViewComponent());
            else if (!visible && hasView) _world.Remove<ViewComponent>(entity);
        }
    }
}
