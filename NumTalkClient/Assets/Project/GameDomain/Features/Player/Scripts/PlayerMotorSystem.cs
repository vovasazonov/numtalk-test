using Arch.Core;
using Project.GameDomain.Features.Course.Scripts;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.PlayerInput.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Player.Scripts
{
    public sealed class PlayerMotorSystem : UnitySystemBase
    {
        private readonly CharacterMotionService _motion;
        private readonly PlatformerTuningConfig _tuning;
        private readonly QueryDescription _players = new QueryDescription().WithAll<PlayerTagComponent, PlayerMotorComponent, CharacterBodyComponent>();
        private readonly ForEach _simulate;
        private float _dt;
        public PlayerMotorSystem(World world, CharacterMotionService motion, PlatformerTuningConfig tuning) : base(world)
        {
            _motion = motion;
            _tuning = tuning;
            _simulate = Simulate;
        }
        public override void Update(in SystemState state)
        {
            _dt = state.DeltaTime;
            if (_dt > 0f) World.Query(in _players, _simulate);
        }
        private void Simulate(Entity entity)
        {
            if (!_motion.IsReady(entity)) return;
            if (World.TryGet(entity, out HealthComponent life) &&
                (life.Phase == PlayerLifePhase.Dying || (life.Phase == PlayerLifePhase.Respawning &&
                 life.PhaseRemaining > _tuning.RespawnBlinkDuration - _tuning.RespawnCameraDuration)))
            {
                // Keep the physical capsule at the ECS respawn pose even while input is locked for the camera glide.
                _motion.Move(entity, World.Get<EntityTransformComponent>(entity).Position, float3.zero, out _, out _);
                return;
            }
            ref var motor = ref World.Get<PlayerMotorComponent>(entity);
            ref var jump = ref World.Get<JumpStateComponent>(entity);
            ref var ground = ref World.Get<GroundStateComponent>(entity);
            ref var external = ref World.Get<ExternalVelocityComponent>(entity);
            ref var rider = ref World.Get<PlatformRiderComponent>(entity);
            ref var pose = ref World.Get<EntityTransformComponent>(entity);
            var input = World.Get<PlayerInputComponent>(entity);
            if (World.TryGet(entity, out RunStateComponent run) && run.IsComplete)
            {
                // Settle naturally onto the finish island, without letting held input walk off its edge.
                input = default;
                motor.Velocity.x = motor.Velocity.z = 0f;
                external.Velocity = float3.zero;
                rider.SurfaceVelocity = float3.zero;
                jump = default;
            }
            bool supported = _motion.Probe(entity, _tuning.GroundProbeDistance, _tuning.GroundProbeMask, out var normal, out var support);
            bool grounded = supported && motor.Velocity.y <= 0f && external.Velocity.y <= 0f;
            if (!grounded) rider.SurfaceVelocity = float3.zero;
            PlayerMotorSimulation.Step(ref motor, ref jump, ref external, ref rider, in input,
                grounded, _motion.CameraForward, _tuning, _dt);
            float3 velocity = motor.Velocity + external.Velocity + rider.SurfaceVelocity;
            motor.PreviousPosition = pose.Position;
            pose.Position = _motion.Move(entity, pose.Position, velocity * _dt, out bool below, out bool above);
            motor.HasSimulationPose = true;
            if (above)
            {
                motor.Velocity.y = math.min(0f, motor.Velocity.y);
                external.Velocity.y = math.min(0f, external.Velocity.y);
            }
            supported = _motion.Probe(entity, _tuning.GroundProbeDistance, _tuning.GroundProbeMask, out normal, out support);
            ground.IsGrounded = (supported || below) && velocity.y <= 0f;
            ground.GroundNormal = supported ? normal : math.up();
            ground.GroundEntity = ground.IsGrounded ? support : default;
            if (ground.IsGrounded)
            {
                motor.Velocity.y = -_tuning.GroundStickSpeed;
                external.Velocity.y = math.max(0f, external.Velocity.y);
            }
            jump.IsAscending = motor.Velocity.y > 0f;
        }
    }
}
