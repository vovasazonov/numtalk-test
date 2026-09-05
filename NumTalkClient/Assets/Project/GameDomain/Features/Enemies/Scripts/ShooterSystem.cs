using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Enemies.Scripts
{
    /// <summary>
    /// Fires pooled projectiles down the authored fire line once the player is inside range. Each shot is preceded
    /// by a wind-up, so the player is warned before anything travels.
    /// </summary>
    public sealed class ShooterSystem : UnitySystemBase
    {
        /// <summary>Spawn distance from the shooter, so a projectile never starts inside its own body.</summary>
        private const float MuzzleOffset = 0.8f;

        private readonly ProjectilePool _pool;
        private readonly PlatformerTuningConfig _tuning;

        private readonly QueryDescription _players = new QueryDescription()
            .WithAll<PlayerTagComponent, EntityTransformComponent>();

        private readonly QueryDescription _shooters = new QueryDescription()
            .WithAll<EnemyComponent, ShooterComponent, EntityTransformComponent>();

        private readonly ForEach _readPlayer;
        private readonly ForEach _step;

        private float3 _playerPosition;
        private bool _hasPlayer;
        private float _dt;

        public ShooterSystem(World world, ProjectilePool pool, PlatformerTuningConfig tuning) : base(world)
        {
            _pool = pool;
            _tuning = tuning;
            _readPlayer = ReadPlayer;
            _step = Step;
        }

        public override void Update(in SystemState state)
        {
            _dt = state.DeltaTime;
            if (_dt <= 0f) return;

            _hasPlayer = false;
            World.Query(in _players, _readPlayer);
            World.Query(in _shooters, _step);
        }

        private void ReadPlayer(Entity entity)
        {
            _playerPosition = World.Get<EntityTransformComponent>(entity).Position;
            _hasPlayer = true;
        }

        private void Step(Entity entity)
        {
            if (World.TryGet(entity, out StompTargetComponent stomp) && stomp.IsDefeated) return;

            ref var shooter = ref World.Get<ShooterComponent>(entity);
            float3 muzzle = World.Get<EntityTransformComponent>(entity).Position
                + shooter.FireDirection * MuzzleOffset;

            if (shooter.WindUpTimer > 0f)
            {
                shooter.WindUpTimer = math.max(0f, shooter.WindUpTimer - _dt);
                if (shooter.WindUpTimer <= 0f) Fire(ref shooter, muzzle);
                return;
            }

            shooter.Cooldown = math.max(0f, shooter.Cooldown - _dt);
            if (shooter.Cooldown > 0f || !IsPlayerOnFireLine(shooter, muzzle)) return;

            shooter.WindUpTimer = shooter.WindUpTime;
            if (shooter.WindUpTimer <= 0f) Fire(ref shooter, muzzle);
        }

        /// <summary>The shooter commits only to what it actually covers: ahead of it, within the authored range.</summary>
        private bool IsPlayerOnFireLine(in ShooterComponent shooter, float3 muzzle)
        {
            if (!_hasPlayer) return false;

            float3 toPlayer = _playerPosition - muzzle;
            float ahead = math.dot(toPlayer, shooter.FireDirection);
            return ahead >= 0f && math.length(toPlayer) <= shooter.Range;
        }

        private void Fire(ref ShooterComponent shooter, float3 muzzle)
        {
            _pool.Rent(muzzle, shooter.FireDirection * shooter.ProjectileSpeed,
                _tuning.ProjectileRadius, _tuning.ProjectileLifeTime);
            shooter.Cooldown = shooter.FireInterval;
        }
    }
}
