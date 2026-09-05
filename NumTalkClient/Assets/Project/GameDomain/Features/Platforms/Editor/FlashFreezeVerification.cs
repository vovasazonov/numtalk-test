using System;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Course.Scripts;
using Project.GameDomain.Features.Enemies.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Project.GameDomain.Features.Platforms.Editor
{
    public static class FlashFreezeVerification
    {
        [MenuItem("NumTalk/Verify Flash Freeze")]
        public static void RunMenu() => Debug.Log(Run());

        public static string Run()
        {
            foreach (int fps in new[] { 30, 60, 120 }) VerifyFreeze(fps);
            VerifyRestore();
            return "Flash freeze passed: warning, timed thaw, composition, checkpoint and full-restart restore.";
        }

        private static void VerifyFreeze(int fps)
        {
            World world = World.Create();
            try
            {
                var freeze = new FlashFreezeComponent { TriggerZ = 10, WarningSeconds = 3, FrozenSeconds = 12, DecelerationScale = 0.1f };
                Entity platform = world.Create(freeze, new PlatformSurfaceComponent { IsStandable = true, SurfaceVelocity = new float3(2, 0, 0) });
                Entity player = world.Create(new PlayerTagComponent(), new EntityTransformComponent { Position = new float3(0, 1, 9) },
                    new GroundStateComponent { IsGrounded = true, GroundEntity = platform }, new PlatformRiderComponent());
                var system = new FlashFreezeSystem(world);
                var rider = new PlatformRiderSystem(world);
                var state = new SystemState { DeltaTime = 1f / fps };
                system.Update(in state);
                Check(world.Get<FlashFreezeComponent>(platform).Phase == FlashFreezePhase.Ready, "Freeze triggered before safe approach");
                world.Get<EntityTransformComponent>(player).Position.z = 10;
                system.Update(in state);
                for (int i = 0; i < fps * 2; i++) system.Update(in state);
                rider.Update(in state);
                Check(world.Get<FlashFreezeComponent>(platform).Phase == FlashFreezePhase.Warning && world.Get<PlatformRiderComponent>(player).SurfaceSlip == 0,
                    "Warning must leave normal traction intact");
                for (int i = 0; i < fps + 2; i++) system.Update(in state);
                rider.Update(in state);
                Check(world.Get<FlashFreezeComponent>(platform).Phase == FlashFreezePhase.Frozen, "Freeze did not follow warning");
                var riding = world.Get<PlatformRiderComponent>(player);
                Check(math.abs(riding.SurfaceSlip - 0.9f) < 0.001f && riding.SurfaceVelocity.x == 2f, "Freeze lost surface carry or slip");
                for (int i = 0; i < fps * 12 + 2; i++) system.Update(in state);
                rider.Update(in state);
                Check(world.Get<FlashFreezeComponent>(platform).Phase == FlashFreezePhase.Thawed && world.Get<PlatformRiderComponent>(player).SurfaceSlip == 0,
                    "Thaw must restore ordinary traction");
                world.Add(platform, new IceSurfaceComponent { DecelerationScale = 0.2f });
                rider.Update(in state);
                Check(math.abs(world.Get<PlatformRiderComponent>(player).SurfaceSlip - 0.8f) < 0.001f, "Thaw removed permanent ice");
                world.Get<PlatformSurfaceComponent>(platform).IsStandable = false;
                rider.Update(in state);
                Check(world.Get<PlatformRiderComponent>(player).SurfaceSlip == 0f, "Unstandable surface still supplies slip");
            }
            finally { World.Destroy(world); }
        }

        private static void VerifyRestore()
        {
            var world = World.Create();
            try
            {
                var bodies = new RigidBodyService();
                var projectiles = new ProjectilePool(world);
                var snapshots = new CourseSnapshotService(world, bodies, projectiles);
                Entity surface = world.Create(new FlashFreezeComponent { WarningSeconds = 3, FrozenSeconds = 12 });
                snapshots.CaptureRunStart();
                world.Get<FlashFreezeComponent>(surface).Phase = FlashFreezePhase.Warning;
                world.Get<FlashFreezeComponent>(surface).Elapsed = 0.7f;
                snapshots.CaptureCheckpoint();
                world.Get<FlashFreezeComponent>(surface).Phase = FlashFreezePhase.Frozen;
                snapshots.RestoreCheckpoint();
                Check(world.Get<FlashFreezeComponent>(surface).Phase == FlashFreezePhase.Warning &&
                    math.abs(world.Get<FlashFreezeComponent>(surface).Elapsed - 0.7f) < 0.001f, "Checkpoint did not restore warning window");
                for (int i = 0; i < 3; i++)
                {
                    world.Get<FlashFreezeComponent>(surface).Phase = FlashFreezePhase.Thawed;
                    snapshots.RestoreRunStart();
                    Check(world.Get<FlashFreezeComponent>(surface).Phase == FlashFreezePhase.Ready, "Restart left stale weather");
                }
            }
            finally { World.Destroy(world); }
        }

        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
