using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.PlayerInput.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.GameDomain.Features.Course.Editor
{
    /// <summary>
    /// A8: measures the swept jump maximums with the shipping tuning asset, then checks every authored gap in
    /// ArenaScene against 75% of the measured horizontal maximum.
    /// </summary>
    public static class CourseMetricsVerification
    {
        private const string ScenePath = "Assets/Project/GameDomain/ScreensDomain/ArenaDomain/Scenes/ArenaScene.unity";
        private const float GapBudget = 0.75f;

        /// <summary>The authored course order. Beat 1 spawn through beat 8 goal.</summary>
        private static readonly string[] Route =
        {
            "Platform_Start", "Platform_Hop1", "Platform_Hop2", "Platform_CrateLanding",
            "Platform_PatrolLedgeA", "Platform_PatrolLedgeB", "Platform_ShooterLane",
            "Platform_MovingIceCarry", "Platform_IceRun", "Platform_IceStep",
            "Platform_Crumble1", "Platform_Crumble2", "Platform_Crumble3", "Platform_CrumbleLanding",
            "Platform_FreezeRun1", "Platform_FreezeRun2", "Platform_FreezeRun3",
            "Platform_FreezeRun4", "Platform_FreezeRun5", "Platform_FreezeRun6",
            "Platform_FreezeRun7", "Platform_FreezeRun8",
            "Platform_MovingGoalFerry", "Platform_GoalApron",
        };

        /// <summary>Platforms the player never has to reach, so no gap budget applies to them.</summary>
        private static readonly string[] OffRoute = { "Platform_ShooterPerch" };

        [MenuItem("NumTalk/Verify Course Metrics")]
        public static void RunMenu() => Debug.Log(Run());

        public static string Run()
        {
            PlatformerTuningConfig tuning = LoadTuning();
            Measure(tuning, out float maximumHeight, out float maximumDistance);
            float budget = maximumDistance * GapBudget;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var report = new StringBuilder();
                report.AppendLine($"A8 passed: measured maximum jump apex {maximumHeight:F2} m and level-to-level " +
                    $"distance {maximumDistance:F2} m at full run speed, so the gap budget is {budget:F2} m.");
                float worst = 0f;
                string worstName = "none";
                foreach ((string from, string to) in Pairs(scene))
                {
                    GameObject a = Find(scene, from);
                    GameObject b = Find(scene, to);
                    float gap = Gap(a, b);
                    float rise = Rise(a, b);
                    Check(gap <= budget, $"{from} -> {to} gap {gap:F2} m exceeds the {budget:F2} m budget");
                    Check(rise <= maximumHeight, $"{from} -> {to} rise {rise:F2} m exceeds the {maximumHeight:F2} m apex");
                    report.AppendLine($"  {from} -> {to}: gap {gap:F2} m ({gap / maximumDistance:P0} of maximum), rise {rise:F2} m");
                    if (gap <= worst) continue;
                    worst = gap;
                    worstName = $"{from} -> {to}";
                }
                report.AppendLine($"  Widest required gap: {worstName} at {worst:F2} m ({worst / maximumDistance:P0} of maximum).");
                return report.ToString();
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>Route pairs, after proving the route covers every authored platform exactly once.</summary>
        private static IEnumerable<(string, string)> Pairs(Scene scene)
        {
            string[] authored = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PlatformBaker>(true))
                .Select(baker => baker.name)
                .OrderBy(name => name)
                .ToArray();
            string[] expected = Route.Concat(OffRoute).OrderBy(name => name).ToArray();
            Check(authored.SequenceEqual(expected),
                "The authored platforms no longer match the route. Authored: " + string.Join(", ", authored));
            return Route.Zip(Route.Skip(1), (from, to) => (from, to));
        }

        private static GameObject Find(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlatformBaker>(true))
            .First(baker => baker.name == name).gameObject;

        /// <summary>Horizontal edge-to-edge distance, taking the most favourable extreme of any moving platform.</summary>
        private static float Gap(GameObject from, GameObject to)
        {
            float best = float.MaxValue;
            foreach (Bounds a in Extremes(from))
            foreach (Bounds b in Extremes(to))
            {
                float x = math.max(0f, math.max(a.min.x - b.max.x, b.min.x - a.max.x));
                float z = math.max(0f, math.max(a.min.z - b.max.z, b.min.z - a.max.z));
                best = math.min(best, math.sqrt(x * x + z * z));
            }
            return best;
        }

        /// <summary>Rise from the highest reachable takeoff surface to the lowest reachable landing surface.</summary>
        private static float Rise(GameObject from, GameObject to) => math.max(0f,
            Extremes(to).Min(b => b.max.y) - Extremes(from).Max(b => b.max.y));

        /// <summary>World bounds at every position the platform occupies. Moving platforms have two.</summary>
        private static Bounds[] Extremes(GameObject platform)
        {
            var renderer = platform.GetComponentInChildren<Renderer>(true);
            Check(renderer != null, platform.name + " has no renderer to measure");
            Check(platform.transform.rotation == Quaternion.identity,
                platform.name + " is rotated, so its axis-aligned bounds would overstate its surface");
            Bounds start = renderer.bounds;
            if (!platform.TryGetComponent(out MovingPlatformBaker moving)) return new[] { start };
            var end = new Bounds(start.center + (moving.EndPosition - platform.transform.position), start.size);
            return new[] { start, end };
        }

        /// <summary>Swept CharacterController jump from a full-speed run, measured on the shipping tuning.</summary>
        private static void Measure(PlatformerTuningConfig tuning, out float maximumHeight, out float maximumDistance)
        {
            World world = World.Create();
            var root = new GameObject("CourseMetricsCharacter");
            var floor = new GameObject("CourseMetricsFloor");
            try
            {
                var origin = new Vector3(20000f, 0f, 20000f);
                floor.layer = 9;
                floor.transform.position = origin + new Vector3(0f, -0.5f, 20f);
                floor.AddComponent<BoxCollider>().size = new Vector3(4f, 1f, 60f);
                root.AddComponent<CharacterController>();
                root.AddComponent<CharacterContactRelay>();
                var child = new GameObject("Body");
                child.transform.SetParent(root.transform, false);
                var listener = child.AddComponent<CharacterBodyComponentListener>();
                var motion = new CharacterMotionService();
                listener.Construct(motion);
                Entity entity = world.Create(new PlayerTagComponent(), new PlayerMotorComponent(), new JumpStateComponent(),
                    new ExternalVelocityComponent(), new PlatformRiderComponent(), new PlayerInputComponent(),
                    new GroundStateComponent(),
                    new EntityTransformComponent { Position = origin, Rotation = quaternion.identity, Layer = 8 },
                    new CharacterBodyComponent
                    {
                        Height = 2f, Radius = 0.4f, Center = new float3(0f, 1f, 0f),
                        SlopeLimit = 50f, StepOffset = 0.35f, SkinWidth = 0.04f,
                    });
                listener.Sync(world, entity);
                root.transform.position = origin;
                UnityEngine.Physics.SyncTransforms();

                var system = new PlayerMotorSystem(world, motion, tuning);
                var state = new SystemState { DeltaTime = 1f / 60f };
                ref var input = ref world.Get<PlayerInputComponent>(entity);
                input = new PlayerInputComponent { Move = new float2(0f, 1f) };
                for (int tick = 0; tick < 60; tick++) system.Update(in state);

                float3 takeoff = world.Get<EntityTransformComponent>(entity).Position;
                Check(world.Get<GroundStateComponent>(entity).IsGrounded, "The run-up never settled on the floor");
                input.JumpPressed = true;
                input.JumpHeld = true;
                maximumHeight = 0f;
                maximumDistance = 0f;
                for (int tick = 0; tick < 240; tick++)
                {
                    system.Update(in state);
                    input.JumpPressed = false;
                    float3 position = world.Get<EntityTransformComponent>(entity).Position;
                    maximumHeight = math.max(maximumHeight, position.y - takeoff.y);
                    if (tick > 0 && world.Get<GroundStateComponent>(entity).IsGrounded)
                    {
                        maximumDistance = math.length((position - takeoff).xz);
                        break;
                    }
                }
                Check(maximumDistance > 0f, "The measured jump never landed back on the floor");
                listener.Release();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(floor);
                World.Destroy(world);
            }
        }

        private static PlatformerTuningConfig LoadTuning()
        {
            string guid = AssetDatabase.FindAssets("t:" + nameof(PlatformerTuningConfig)).FirstOrDefault();
            Check(guid != null, "No PlatformerTuningConfig asset exists; A8 must measure the shipping values");
            return AssetDatabase.LoadAssetAtPath<PlatformerTuningConfig>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("A8 verification failed: " + label);
        }
    }
}
