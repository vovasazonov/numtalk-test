using System.Collections.Generic;
using Arch.Core;
using Project.GameDomain.Features.Checkpoints.Scripts;
using Project.GameDomain.Features.Enemies.Scripts;
using Project.GameDomain.Features.Pickup.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Course.Scripts
{
    /// <summary>
    /// A deterministic copy of everything the course mutates while it is played. Held as plain lists so a capture
    /// is a value copy, never a reference into the world.
    /// </summary>
    public sealed class CourseSnapshot
    {
        public readonly List<(Entity Entity, EntityTransformComponent Pose, bool HasView)> Poses = new();
        public readonly List<(Entity Entity, float3 Position, quaternion Rotation)> Bodies = new();
        public readonly List<(Entity Entity, PlatformMotionComponent Value)> Platforms = new();
        public readonly List<(Entity Entity, CrumbleStateComponent Value)> Crumbles = new();
        public readonly List<(Entity Entity, FlashFreezeComponent Value)> Freezes = new();
        public readonly List<(Entity Entity, PatrolComponent Value)> Patrols = new();
        public readonly List<(Entity Entity, ShooterComponent Value)> Shooters = new();
        public readonly List<(Entity Entity, StompTargetComponent Value)> StompTargets = new();
        public readonly List<(Entity Entity, CheckpointComponent Value)> Checkpoints = new();
        public readonly List<(Entity Entity, PickupComponent Value, bool HasView)> Pickups = new();

        public void Clear()
        {
            Poses.Clear();
            Bodies.Clear();
            Platforms.Clear();
            Crumbles.Clear();
            Freezes.Clear();
            Patrols.Clear();
            Shooters.Clear();
            StompTargets.Clear();
            Checkpoints.Clear();
            Pickups.Clear();
        }
    }
}
