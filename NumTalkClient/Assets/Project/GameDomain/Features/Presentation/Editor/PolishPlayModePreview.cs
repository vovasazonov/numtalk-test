using System;
using Arch.Core;
using Project.GameDomain.Features.Course.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Project.GameDomain.ScreensDomain.ArenaDomain.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace Project.GameDomain.Features.Presentation.Editor
{
    /// <summary>Editor-only inspection shortcuts. Exercise the real arena world, never included in a player build.</summary>
    public static class PolishPlayModePreview
    {
        [MenuItem("NumTalk/Preview/Freeze Approach (Play Mode)")]
        public static void FreezeApproach() => Place(new float3(12.5f, 1.6f, 113.7f));

        [MenuItem("NumTalk/Preview/Goal (Play Mode)")]
        public static void Goal() => Place(new float3(20f, 1.6f, 174f));

        [MenuItem("NumTalk/Preview/Shooter (Play Mode)")]
        public static void Shooter() => Place(new float3(-1f, 1.6f, 57f));

        private static World ArenaWorld()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Launch the course first.");
            var scope = UnityEngine.Object.FindFirstObjectByType<ArenaScreenScope>();
            if (scope == null) throw new InvalidOperationException("Enter the arena first.");
            return scope.Container.Resolve<World>();
        }

        private static void Place(float3 position)
        {
            var world = ArenaWorld();
            var query = new QueryDescription().WithAll<PlayerTagComponent, EntityTransformComponent>();
            world.Query(in query, (Entity entity) =>
            {
                world.Get<EntityTransformComponent>(entity).Position = position;
                world.Get<PlayerMotorComponent>(entity) = default;
                world.Get<GroundStateComponent>(entity) = default;
                world.Get<ExternalVelocityComponent>(entity) = default;
                world.Get<PlatformRiderComponent>(entity) = default;
            });
        }

        public static string State()
        {
            var world = ArenaWorld();
            string report = "";
            var query = new QueryDescription().WithAll<PlayerTagComponent, EntityTransformComponent>();
            world.Query(in query, (Entity e) => report = $"Player {world.Get<EntityTransformComponent>(e).Position}; " +
                $"grounded {world.Get<GroundStateComponent>(e).IsGrounded}; complete {world.Get<RunStateComponent>(e).IsComplete}; " +
                $"lives {world.Get<HealthComponent>(e).Lives}. ");
            int warning = 0, frozen = 0;
            var weather = new QueryDescription().WithAll<FlashFreezeComponent>();
            world.Query(in weather, (Entity e) =>
            {
                var phase = world.Get<FlashFreezeComponent>(e).Phase;
                if (phase == FlashFreezePhase.Warning) warning++;
                if (phase == FlashFreezePhase.Frozen) frozen++;
            });
            return report + $"Warning surfaces {warning}; frozen surfaces {frozen}.";
        }
    }
}
