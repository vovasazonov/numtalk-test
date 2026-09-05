using System;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.PlayerInput.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Project.GameDomain.Features.Player.Editor
{
    /// <summary>Repeatable A6 regression checks; temporary physics objects are always removed.</summary>
    public static class PlayerMotorVerification
    {
        [MenuItem("NumTalk/Verify Player Motor")]
        public static void RunMenu() => Debug.Log(Run());

        public static string Run()
        {
            var tuning = ScriptableObject.CreateInstance<PlatformerTuningConfig>();
            try
            {
                float held = JumpHeight(tuning, false);
                float cut = JumpHeight(tuning, true);
                Check(held > 2.4f && held < 2.7f && cut < held * 0.7f, "Variable jump apex");
                var motor = new PlayerMotorComponent();
                var jump = new JumpStateComponent { CoyoteTimer = tuning.CoyoteTime };
                var external = new ExternalVelocityComponent();
                var rider = new PlatformRiderComponent();
                var input = new PlayerInputComponent { JumpPressed = true, JumpHeld = true };
                Step(ref motor, ref jump, ref external, ref rider, input, false, tuning);
                Check(motor.Velocity.y > 0f && jump.CoyoteTimer == 0f, "Coyote jump consumed once");
                motor = default; jump = default;
                Step(ref motor, ref jump, ref external, ref rider, input, false, tuning);
                Check(motor.Velocity.y < 0f, "No midair jump");
                input.JumpPressed = false;
                Step(ref motor, ref jump, ref external, ref rider, input, true, tuning);
                Check(motor.Velocity.y > 0f && jump.BufferTimer == 0f, "Buffered landing jump");
                motor = default; jump = new JumpStateComponent { CoyoteTimer = 0.001f };
                input = new PlayerInputComponent { JumpPressed = true, JumpHeld = true };
                Step(ref motor, ref jump, ref external, ref rider, input, false, tuning);
                Check(motor.Velocity.y < 0f, "Expired coyote cannot jump");
                for (int i = 0; i < 20; i++)
                {
                    input.JumpPressed = false;
                    Step(ref motor, ref jump, ref external, ref rider, input, false, tuning);
                }
                Step(ref motor, ref jump, ref external, ref rider, input, true, tuning);
                Check(motor.Velocity.y < 0f, "Expired buffer and held button cannot auto-jump");
                motor = default; jump = default; input = new PlayerInputComponent { Move = new float2(0, 1) };
                PlayerMotorSimulation.Step(ref motor, ref jump, ref external, ref rider, input, true,
                    new float3(1, 0, 0), tuning, 1f / 60f);
                Check(motor.Velocity.x > 0f && math.abs(motor.Velocity.z) < 0.001f, "Camera-relative movement");
                for (int i = 0; i < 120; i++) Step(ref motor, ref jump, ref external, ref rider, input, false, tuning);
                Check(math.abs(motor.Velocity.y + tuning.TerminalFallSpeed) < 0.001f, "Terminal speed");
                input = default;
                for (int i = 0; i < 60; i++) Step(ref motor, ref jump, ref external, ref rider, input, true, tuning);
                Check(math.length(motor.Velocity.xz) < 0.001f, "Ground deceleration");
                external.Velocity = new float3(9, 0, 0);
                PlayerMotorSimulation.Step(ref motor, ref jump, ref external, ref rider, input, false,
                    new float3(0, 0, 1), tuning, tuning.AirborneKnockbackHalfLife);
                Check(math.abs(external.Velocity.x - 4.5f) < 0.001f, "Independent impulse half-life");
                motor = default; jump = default; external = default;
                rider.SurfaceVelocity = new float3(3, -4, 0);
                input = new PlayerInputComponent { JumpPressed = true, JumpHeld = true };
                Step(ref motor, ref jump, ref external, ref rider, input, true, tuning);
                Check(external.Velocity.x > 2f && external.Velocity.y == 0f && math.lengthsq(rider.SurfaceVelocity) == 0f,
                    "Jump-off inheritance excludes downward velocity");
                VerifyContacts(tuning);
                return $"A6 passed: camera-relative acceleration, stop, coyote, buffer, jump cut (held {held:F3}m / cut {cut:F3}m), terminal speed, impulse half-life, platform inheritance, ground/wall/ceiling contacts, 10 thin-platform falls each at simulated 30/60/120 FPS, bridge release.";
            }
            finally { UnityEngine.Object.DestroyImmediate(tuning); }
        }

        private static void VerifyContacts(PlatformerTuningConfig tuning)
        {
            World world = World.Create();
            var root = new GameObject("MotorVerificationCharacter");
            var floor = new GameObject("MotorVerificationFloor");
            var wall = new GameObject("MotorVerificationWall");
            try
            {
                floor.layer = 9;
                floor.transform.position = new Vector3(10000, -0.05f, 10000);
                floor.AddComponent<BoxCollider>().size = new Vector3(10, 0.1f, 10);
                wall.layer = 9;
                wall.transform.position = new Vector3(10002, 2, 10000);
                wall.AddComponent<BoxCollider>().size = new Vector3(0.1f, 4, 10);
                root.AddComponent<CharacterController>();
                var child = new GameObject("Body"); child.transform.SetParent(root.transform, false);
                var listener = child.AddComponent<CharacterBodyComponentListener>();
                var motion = new CharacterMotionService(); listener.Construct(motion);
                Entity entity = world.Create(new PlayerTagComponent(), new PlayerMotorComponent(), new JumpStateComponent(),
                    new ExternalVelocityComponent(), new PlatformRiderComponent(), new PlayerInputComponent(),
                    new GroundStateComponent(), new EntityTransformComponent { Position = new float3(10000, 3, 10000), Rotation = quaternion.identity, Layer = 8 },
                    new CharacterBodyComponent { Height = 2, Radius = 0.4f, Center = new float3(0, 1, 0), SlopeLimit = 50, StepOffset = 0.35f, SkinWidth = 0.04f });
                listener.Sync(world, entity);
                var system = new PlayerMotorSystem(world, motion, tuning);
                var state = new SystemState { DeltaTime = 1f / 60f };
                foreach (int renderRate in new[] { 30, 60, 120 })
                for (int trial = 0; trial < 10; trial++)
                {
                    world.Get<EntityTransformComponent>(entity).Position = new float3(10000, 3, 10000);
                    world.Get<PlayerMotorComponent>(entity).Velocity = new float3(0, -32, 0);
                    root.transform.position = new Vector3(10000, 3, 10000);
                    UnityEngine.Physics.SyncTransforms();
                    double accumulator = 0;
                    for (int frame = 0; frame < renderRate; frame++)
                    {
                        accumulator += 1.0 / renderRate;
                        while (accumulator + 0.0000001 >= state.DeltaTime)
                        {
                            system.Update(in state);
                            accumulator -= state.DeltaTime;
                        }
                    }
                    Check(world.Get<EntityTransformComponent>(entity).Position.y >= -0.08f && world.Get<GroundStateComponent>(entity).IsGrounded,
                        "Thin platform terminal fall " + trial + " at simulated " + renderRate + " FPS");
                }
                float3 position = world.Get<EntityTransformComponent>(entity).Position;
                position = motion.Move(entity, position, new float3(5, 0, 0), out _, out _);
                Check(position.x < 10001.7f, "Swept wall contact");
                wall.transform.position = new Vector3(10000, 3, 10000);
                wall.GetComponent<BoxCollider>().size = new Vector3(10, 0.1f, 10);
                UnityEngine.Physics.SyncTransforms();
                motion.Move(entity, new float3(10000, 0, 10000), new float3(0, 4, 0), out _, out bool above);
                Check(above, "Swept ceiling contact");
                listener.Release();
                child.SetActive(false);
                Check(!motion.IsReady(entity), "Pooled bridge unregisters");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(floor);
                UnityEngine.Object.DestroyImmediate(wall);
                World.Destroy(world);
            }
        }

        private static float JumpHeight(PlatformerTuningConfig tuning, bool cut)
        {
            var motor = new PlayerMotorComponent(); var jump = new JumpStateComponent();
            var external = new ExternalVelocityComponent(); var rider = new PlatformRiderComponent();
            float y = 0f, apex = 0f;
            for (int tick = 0; tick < 120; tick++)
            {
                var input = new PlayerInputComponent { JumpPressed = tick == 0, JumpHeld = !cut || tick < 3, JumpReleased = cut && tick == 3 };
                Step(ref motor, ref jump, ref external, ref rider, input, tick == 0, tuning);
                y += motor.Velocity.y / 60f; apex = math.max(apex, y);
            }
            return apex;
        }
        private static void Step(ref PlayerMotorComponent motor, ref JumpStateComponent jump,
            ref ExternalVelocityComponent external, ref PlatformRiderComponent rider, PlayerInputComponent input,
            bool grounded, PlatformerTuningConfig tuning)
            => PlayerMotorSimulation.Step(ref motor, ref jump, ref external, ref rider, input, grounded, new float3(0, 0, 1), tuning, 1f / 60f);
        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("A6 verification failed: " + label);
        }
    }
}
