using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.PlayerInput.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Player.Scripts
{
    /// <summary>Fixed-step velocity integration, independent of Unity physics and presentation.</summary>
    public static class PlayerMotorSimulation
    {
        public static void Step(ref PlayerMotorComponent motor, ref JumpStateComponent jump,
            ref ExternalVelocityComponent external, ref PlatformRiderComponent rider,
            in PlayerInputComponent input, bool grounded, float3 forward, PlatformerTuningConfig tuning, float dt)
        {
            jump.CoyoteTimer = grounded ? tuning.CoyoteTime : math.max(0f, jump.CoyoteTimer - dt);
            jump.BufferTimer = input.JumpPressed ? tuning.JumpBufferTime : math.max(0f, jump.BufferTimer - dt);
            jump.IsHeld = input.JumpHeld;
            forward.y = 0f;
            forward = math.normalizesafe(forward, new float3(0f, 0f, 1f));
            float3 right = new float3(forward.z, 0f, -forward.x);
            float2 intent = input.Move / math.max(1f, math.length(input.Move));
            float3 target = (right * intent.x + forward * intent.y) * tuning.MaximumRunSpeed;
            float3 horizontal = new float3(motor.Velocity.x, 0f, motor.Velocity.z);
            // Ice removes only deceleration, so intent still accelerates at full strength but momentum carries.
            float acceleration = math.lengthsq(intent) > 0f
                ? tuning.GroundAcceleration
                : tuning.GroundDeceleration * (1f - rider.SurfaceSlip);
            acceleration *= grounded ? 1f : tuning.AirAccelerationScale;
            float3 difference = target - horizontal;
            horizontal += math.normalizesafe(difference) * math.min(math.length(difference), acceleration * dt);
            motor.Velocity.x = horizontal.x;
            motor.Velocity.z = horizontal.z;

            if (grounded && motor.Velocity.y < 0f) motor.Velocity.y = -tuning.GroundStickSpeed;
            bool launch = jump.BufferTimer > 0f && (grounded || jump.CoyoteTimer > 0f);
            if (launch)
            {
                motor.Velocity.y = math.sqrt(2f * tuning.AscentGravity * tuning.TargetJumpApexHeight);
                float3 inherited = rider.SurfaceVelocity;
                inherited.y = math.max(0f, inherited.y);
                inherited *= math.min(1f, tuning.MaximumInheritedPlatformSpeed / math.max(0.0001f, math.length(inherited)));
                external.Velocity += inherited;
                rider.SurfaceVelocity = float3.zero;
                rider.Platform = default;
                jump.BufferTimer = 0f;
                jump.CoyoteTimer = 0f;
            }
            if (motor.Velocity.y > 0f && (input.JumpReleased || (launch && !input.JumpHeld)))
                motor.Velocity.y *= tuning.EarlyReleaseVelocityCut;
            float gravity = tuning.AscentGravity * (motor.Velocity.y > 0f ? 1f : tuning.FallGravityMultiplier);
            motor.Velocity.y = math.max(-tuning.TerminalFallSpeed, motor.Velocity.y - gravity * dt);
            jump.IsAscending = motor.Velocity.y > 0f;
            float halfLife = grounded && !launch ? tuning.GroundedKnockbackHalfLife : tuning.AirborneKnockbackHalfLife;
            external.Velocity *= halfLife > 0f ? math.exp2(-dt / halfLife) : 0f;
        }
    }
}
