using System;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.Course.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.PlayerInput.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Project.GameDomain.Features.Platforms.Editor
{
    /// <summary>A10 regression checks. Pure ECS, so no Unity physics objects are created.</summary>
    public static class PlatformBehaviorVerification
    {
        private const float Dt = 1f / 60f;

        [MenuItem("NumTalk/Verify Platform Behavior")]
        public static void RunMenu() => Debug.Log(Run());

        public static string Run()
        {
            var tuning = ScriptableObject.CreateInstance<PlatformerTuningConfig>();
            World world = World.Create();
            try
            {
                var motion = new MovingPlatformSystem(world);
                var rider = new PlatformRiderSystem(world);
                var state = new SystemState { DeltaTime = Dt };

                // A route travels at the authored speed, reports it as surface velocity, then reverses and waits.
                Entity platform = NewPlatform(world, new float3(0f, 1f, 0f));
                world.Add(platform, new PlatformMotionComponent
                {
                    StartPosition = new float3(0f, 1f, 0f), EndPosition = new float3(4f, 1f, 0f),
                    Speed = 2f, WaitTime = 0.5f, IsForward = true,
                });
                motion.Update(in state);
                Check(math.abs(world.Get<PlatformSurfaceComponent>(platform).SurfaceVelocity.x - 2f) < 0.001f,
                    "Surface velocity matches the authored speed on the first tick");
                // 4 m at 2 m/s is 120 ticks out, then a 30 tick wait. Sample inside that wait.
                for (int tick = 1; tick < 130; tick++) motion.Update(in state);
                Check(math.abs(world.Get<EntityTransformComponent>(platform).Position.x - 4f) < 0.001f &&
                    world.Get<PlatformMotionComponent>(platform).WaitTimer > 0f &&
                    !world.Get<PlatformMotionComponent>(platform).IsForward, "Route reverses and waits at the end");
                // 30 tick wait, then 120 ticks back, so the platform is home and heading out again.
                for (int tick = 0; tick < 155; tick++) motion.Update(in state);
                Check(math.abs(world.Get<EntityTransformComponent>(platform).Position.x) < 0.001f &&
                    world.Get<PlatformMotionComponent>(platform).IsForward, "Route returns to its start and repeats");

                // A route that starts part-way along must not report the offset as a one-tick teleport.
                Entity offset = NewPlatform(world, float3.zero);
                world.Add(offset, new PlatformMotionComponent
                {
                    StartPosition = float3.zero, EndPosition = new float3(10f, 0f, 0f),
                    Speed = 2f, Progress = 0.5f, IsForward = true,
                });
                motion.Update(in state);
                Check(math.length(world.Get<PlatformSurfaceComponent>(offset).SurfaceVelocity) < 2.001f,
                    "A part-way start does not report a teleport as surface velocity");

                // Composition: one entity carrying motion and ice supplies both channels to the same rider.
                Entity movingIce = NewPlatform(world, float3.zero);
                world.Add(movingIce, new PlatformMotionComponent
                {
                    StartPosition = float3.zero, EndPosition = new float3(0f, 0f, 6f), Speed = 3f, IsForward = true,
                });
                world.Add(movingIce, new IceSurfaceComponent { DecelerationScale = 0.1f });
                motion.Update(in state);
                Entity player = world.Create(new PlayerTagComponent(), new PlayerMotorComponent(), new JumpStateComponent(),
                    new ExternalVelocityComponent(), new PlatformRiderComponent(), new PlayerInputComponent(),
                    new GroundStateComponent { IsGrounded = true, GroundEntity = movingIce });
                rider.Update(in state);
                var ridden = world.Get<PlatformRiderComponent>(player);
                Check(math.abs(ridden.SurfaceVelocity.z - 3f) < 0.001f, "Moving+ice platform carries the rider");
                Check(math.abs(ridden.SurfaceSlip - 0.9f) < 0.001f, "Moving+ice platform also makes the rider slip");

                // Ice removes deceleration only. Intent still accelerates at full strength.
                Check(Stop(tuning, 0.9f) > Stop(tuning, 0f) * 5f, "Ice keeps momentum far longer than normal ground");
                Check(math.abs(Accelerate(tuning, 0.9f) - Accelerate(tuning, 0f)) < 0.001f,
                    "Ice does not weaken deliberate acceleration");

                // A rider standing on nothing, on a dead entity, or on an unstandable surface gets no channel at all.
                world.Get<PlatformSurfaceComponent>(movingIce).IsStandable = false;
                rider.Update(in state);
                Check(math.lengthsq(world.Get<PlatformRiderComponent>(player).SurfaceVelocity) == 0f &&
                    world.Get<PlatformRiderComponent>(player).SurfaceSlip == 0f,
                    "An unstandable surface supplies neither velocity nor slip");
                motion.Update(in state);
                Check(math.lengthsq(world.Get<PlatformSurfaceComponent>(movingIce).SurfaceVelocity) == 0f,
                    "Motion yields on a surface that has given way");
                world.Get<GroundStateComponent>(player).IsGrounded = false;
                world.Get<PlatformSurfaceComponent>(movingIce).IsStandable = true;
                rider.Update(in state);
                Check(math.lengthsq(world.Get<PlatformRiderComponent>(player).SurfaceVelocity) == 0f,
                    "An airborne rider inherits nothing");

                VerifyCrumble(world, rider);

                return "A10 passed: authored routes, surface velocity without teleport spikes, moving+ice composed on " +
                    "one entity, ice removing deceleration only, unstandable/airborne riders inheriting nothing, and " +
                    "the crumble phase cycle composing onto a moving platform without touching the other behaviours.";
            }
            finally
            {
                World.Destroy(world);
                UnityEngine.Object.DestroyImmediate(tuning);
            }
        }


        /// <summary>The fourth behaviour, added on top of the shared surface contract without changing it.</summary>
        private static void VerifyCrumble(World world, PlatformRiderSystem rider)
        {
            var crumble = new CrumblePlatformSystem(world);
            var motion = new MovingPlatformSystem(world);
            var state = new SystemState { DeltaTime = Dt };

            Entity platform = NewPlatform(world, new float3(0f, 1f, 0f));
            world.Add(platform, new InitialStateComponent { Position = new float3(0f, 1f, 0f), Rotation = quaternion.identity });
            world.Add(platform, new CrumbleStateComponent
            {
                Phase = CrumblePhase.Stable, TelegraphTime = 0.35f, FallDelay = 0.55f, RespawnTime = 3f,
            });
            // Composed onto a moving route, proving crumble is not a forked prefab family either.
            world.Add(platform, new PlatformMotionComponent
            {
                StartPosition = new float3(0f, 1f, 0f), EndPosition = new float3(0f, 1f, 6f), Speed = 2f, IsForward = true,
            });
            Entity player = world.Create(new PlayerTagComponent(), new PlatformRiderComponent(),
                new GroundStateComponent { IsGrounded = false });

            crumble.Update(in state);
            Check(world.Get<CrumbleStateComponent>(platform).Phase == CrumblePhase.Stable, "Crumble is stable until stood on");

            world.Get<GroundStateComponent>(player) = new GroundStateComponent { IsGrounded = true, GroundEntity = platform };
            crumble.Update(in state);
            Check(world.Get<CrumbleStateComponent>(platform).Phase == CrumblePhase.Telegraphing, "Standing on it starts the warning");
            Check(world.Get<PlatformSurfaceComponent>(platform).IsStandable, "The warning is survivable, not an instant drop");

            // The player steps off during the warning; the tell must still run to completion.
            world.Get<GroundStateComponent>(player).IsGrounded = false;
            for (int tick = 0; tick < 54; tick++) crumble.Update(in state);
            Check(world.Get<CrumbleStateComponent>(platform).Phase == CrumblePhase.Telegraphing,
                "Telegraph plus fall delay is 0.9 s, so it has not fallen at 0.9 s");
            crumble.Update(in state);
            Check(world.Get<CrumbleStateComponent>(platform).Phase == CrumblePhase.Falling &&
                !world.Get<PlatformSurfaceComponent>(platform).IsStandable, "It gives way after the authored delay");

            float3 before = world.Get<EntityTransformComponent>(platform).Position;
            motion.Update(in state);
            Check(math.abs(world.Get<EntityTransformComponent>(platform).Position.z - before.z) < 0.001f &&
                math.lengthsq(world.Get<PlatformSurfaceComponent>(platform).SurfaceVelocity) == 0f,
                "Motion yields to crumble on the same entity rather than fighting it");
            crumble.Update(in state);
            Check(world.Get<EntityTransformComponent>(platform).Position.y < before.y, "A fallen platform drops away");

            rider.Update(in state);
            Check(math.lengthsq(world.Get<PlatformRiderComponent>(player).SurfaceVelocity) == 0f,
                "A fallen platform carries nobody");

            for (int tick = 0; tick < 240; tick++) crumble.Update(in state);
            Check(world.Get<CrumbleStateComponent>(platform).Phase == CrumblePhase.Stable &&
                world.Get<PlatformSurfaceComponent>(platform).IsStandable &&
                math.distance(world.Get<EntityTransformComponent>(platform).Position, new float3(0f, 1f, 0f)) < 0.001f,
                "It respawns stable at its authored pose");
        }

        /// <summary>Distance covered coasting to a stop from top speed with no input, at the given slip.</summary>
        private static float Stop(PlatformerTuningConfig tuning, float slip)
        {
            var motor = new PlayerMotorComponent { Velocity = new float3(0f, 0f, tuning.MaximumRunSpeed) };
            var jump = new JumpStateComponent();
            var external = new ExternalVelocityComponent();
            var rider = new PlatformRiderComponent { SurfaceSlip = slip };
            float distance = 0f;
            for (int tick = 0; tick < 600 && math.abs(motor.Velocity.z) > 0.01f; tick++)
            {
                PlayerMotorSimulation.Step(ref motor, ref jump, ref external, ref rider, default,
                    true, new float3(0f, 0f, 1f), tuning, Dt);
                distance += motor.Velocity.z * Dt;
            }
            return distance;
        }

        /// <summary>Speed reached from rest after one second of full intent, at the given slip.</summary>
        private static float Accelerate(PlatformerTuningConfig tuning, float slip)
        {
            var motor = new PlayerMotorComponent();
            var jump = new JumpStateComponent();
            var external = new ExternalVelocityComponent();
            var rider = new PlatformRiderComponent { SurfaceSlip = slip };
            var input = new PlayerInputComponent { Move = new float2(0f, 1f) };
            for (int tick = 0; tick < 60; tick++)
                PlayerMotorSimulation.Step(ref motor, ref jump, ref external, ref rider, input,
                    true, new float3(0f, 0f, 1f), tuning, Dt);
            return math.length(motor.Velocity.xz);
        }

        private static Entity NewPlatform(World world, float3 position) => world.Create(
            new PlatformSurfaceComponent { IsStandable = true },
            new EntityTransformComponent { Position = position, Rotation = quaternion.identity, Layer = 10 });

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("A10 verification failed: " + label);
        }
    }
}
