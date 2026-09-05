using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Course.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    /// <summary>
    /// Stable -> Telegraphing -> Falling -> Respawning, driven by whoever is standing on the surface. The fourth
    /// behaviour: one component, one system, and one registration, with no change to the motor or the other
    /// platform behaviours.
    /// </summary>
    public sealed class CrumblePlatformSystem : UnitySystemBase
    {
        /// <summary>Downward speed of a platform that has given way, in metres per second.</summary>
        private const float FallSpeed = 6f;

        private readonly QueryDescription _crumbling = new QueryDescription()
            .WithAll<CrumbleStateComponent, PlatformSurfaceComponent, EntityTransformComponent, InitialStateComponent>();

        private readonly QueryDescription _riders = new QueryDescription()
            .WithAll<PlayerTagComponent, GroundStateComponent>();

        private readonly ForEach _advance;
        private readonly ForEach _collectRider;

        private Entity _stoodOn;
        private float _dt;

        public CrumblePlatformSystem(World world) : base(world)
        {
            _advance = Advance;
            _collectRider = CollectRider;
        }

        public override void Update(in SystemState state)
        {
            _dt = state.DeltaTime;
            if (_dt <= 0f) return;

            _stoodOn = default;
            World.Query(in _riders, _collectRider);
            World.Query(in _crumbling, _advance);
        }

        private void CollectRider(Entity entity)
        {
            ref var ground = ref World.Get<GroundStateComponent>(entity);
            if (ground.IsGrounded) _stoodOn = ground.GroundEntity;
        }

        private void Advance(Entity entity)
        {
            ref var crumble = ref World.Get<CrumbleStateComponent>(entity);
            ref var surface = ref World.Get<PlatformSurfaceComponent>(entity);
            ref var pose = ref World.Get<EntityTransformComponent>(entity);

            crumble.PhaseTimer += _dt;

            switch (crumble.Phase)
            {
                case CrumblePhase.Stable:
                    if (entity != _stoodOn) return;
                    Enter(ref crumble, CrumblePhase.Telegraphing);
                    return;

                case CrumblePhase.Telegraphing:
                    // The warning runs to completion even if the player steps off, so the tell is never a lie.
                    if (crumble.PhaseTimer < crumble.TelegraphTime + crumble.FallDelay) return;
                    Enter(ref crumble, CrumblePhase.Falling);
                    surface.IsStandable = false;
                    return;

                case CrumblePhase.Falling:
                    pose.Position.y -= FallSpeed * _dt;
                    if (crumble.PhaseTimer < crumble.RespawnTime) return;
                    Enter(ref crumble, CrumblePhase.Respawning);
                    return;

                case CrumblePhase.Respawning:
                    // Never restore the surface under a player who has since landed where it used to be.
                    if (entity == _stoodOn) return;
                    pose.Position = World.Get<InitialStateComponent>(entity).Position;
                    surface.IsStandable = true;
                    Enter(ref crumble, CrumblePhase.Stable);
                    return;
            }
        }

        private static void Enter(ref CrumbleStateComponent crumble, CrumblePhase phase)
        {
            crumble.Phase = phase;
            crumble.PhaseTimer = 0f;
        }
    }
}
