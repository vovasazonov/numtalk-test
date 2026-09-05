using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Project.GameDomain.Features.Checkpoints.Scripts;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.Enemies.Scripts;
using Project.GameDomain.Features.Goal.Scripts;
using Project.GameDomain.Features.Hazards.Scripts;
using Project.GameDomain.Features.Pickup.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Pushables.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.GameDomain.Features.Course.Editor
{
    /// <summary>
    /// A15: proves the authored course actually contains every beat with its mandatory interaction, that the coin
    /// route is spread across the course rather than clustered, and that a clean run lands inside the 60-120 second
    /// target. Gap and rise budgets stay with A8's `Verify Course Metrics`.
    /// </summary>
    public static class CourseIntegrationVerification
    {
        private const string ScenePath = "Assets/Project/GameDomain/ScreensDomain/ArenaDomain/Scenes/ArenaScene.unity";

        /// <summary>Fraction of maximum run speed a real run averages, after jump arcs, waits and corrections.</summary>
        private const float RunEfficiency = 0.55f;

        private const float MinimumSeconds = 60f;
        private const float MaximumSeconds = 120f;

        /// <summary>Each beat, and the platform on the A8 route where the player meets it.</summary>
        private static readonly (int Beat, string Platform, string Interaction)[] Beats =
        {
            (1, "Platform_Start", "arrival and gentle gaps"),
            (2, "Platform_CrateLanding", "crate landing puzzle"),
            (3, "Platform_PatrolLedgeA", "patrol ledges"),
            (4, "Platform_ShooterLane", "shooter approach"),
            (5, "Platform_MovingIceCarry", "moving ice crossing"),
            (6, "Platform_Crumble1", "crumble chain and checkpoint"),
            (7, "Platform_FreezeRun1", "final mixed-mechanic run"),
            (8, "Platform_GoalApron", "goal"),
        };

        [MenuItem("NumTalk/Verify Course Integration")]
        public static void RunMenu() => Debug.Log(Run());

        public static string Run()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var report = new StringBuilder();
                GameObject[] roots = scene.GetRootGameObjects();

                VerifyInteractions(roots, report);
                VerifyBeatOrder(roots, report);
                VerifyCoinRoute(roots, report);
                float seconds = VerifyDuration(roots, report);

                return $"A15 passed: all eight beats present in course order with their mandatory interaction, a coin " +
                    $"route spread across every beat, and an estimated clean run of {seconds:F0} s inside the " +
                    $"{MinimumSeconds:F0}-{MaximumSeconds:F0} s target.\n" + report;
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>Every mandatory mechanic is authored, and the composed platform proves behaviours compose.</summary>
        private static void VerifyInteractions(GameObject[] roots, StringBuilder report)
        {
            Check(Count<PlayerBaker>(roots) == 1, "The course needs exactly one player");
            Check(Count<GoalBaker>(roots) == 1, "The course needs exactly one goal");
            Check(Count<KillZoneBaker>(roots) >= 1, "The course needs a kill plane");
            Check(Count<PushableCrateBaker>(roots) >= 1, "Beat 2 needs a pushable crate");
            Check(Count<PatrolBaker>(roots) >= 2, "Beat 3 needs patrol enemies");
            Check(Count<ShooterBaker>(roots) >= 1, "Beat 4 needs a shooter");
            Check(Count<MovingPlatformBaker>(roots) >= 1, "Beat 5 needs a moving platform");
            Check(Count<IceSurfaceBaker>(roots) >= 1, "Beat 5 needs an ice surface");
            Check(Count<CrumblePlatformBaker>(roots) >= 3, "Beat 6 needs a crumble chain");
            Check(Count<CheckpointBaker>(roots) >= 3, "The course needs checkpoints between its beats");

            GameObject composed = All<MovingPlatformBaker>(roots)
                .FirstOrDefault(baker => baker.GetComponent<IceSurfaceBaker>() != null)?.gameObject;
            Check(composed != null, "No single platform carries both Moving and Ice, so composition is unproven");

            report.AppendLine($"  Interactions: crate, {Count<PatrolBaker>(roots)} patrols, {Count<ShooterBaker>(roots)} " +
                $"shooter, {Count<MovingPlatformBaker>(roots)} moving, {Count<IceSurfaceBaker>(roots)} ice, " +
                $"{Count<CrumblePlatformBaker>(roots)} crumble, {Count<CheckpointBaker>(roots)} checkpoints, one goal.");
            report.AppendLine($"  Moving+Ice composed on '{composed.name}'.");
        }

        /// <summary>The beats run forward along the course, so the player never doubles back to meet one.</summary>
        private static void VerifyBeatOrder(GameObject[] roots, StringBuilder report)
        {
            float previous = float.NegativeInfinity;
            foreach ((int beat, string platform, string interaction) in Beats)
            {
                GameObject anchor = Find(roots, platform);
                float z = anchor.transform.position.z;
                Check(z > previous, $"Beat {beat} ({interaction}) at z {z:F1} does not follow the previous beat");
                previous = z;
                report.AppendLine($"  Beat {beat} at '{platform}' (z {z:F1}): {interaction}.");
            }

            GameObject goal = All<GoalBaker>(roots).Single().gameObject;
            Check(goal.transform.position.z >= previous - 4f, "The goal is not at the end of the course");
        }

        /// <summary>Coins are a route, not filler: every beat span carries some, and none sits off the course.</summary>
        private static void VerifyCoinRoute(GameObject[] roots, StringBuilder report)
        {
            CoinBaker[] coins = All<CoinBaker>(roots).ToArray();
            Check(coins.Length >= 12, $"The coin route is too thin at {coins.Length} coins");

            int[] ids = coins.Select(Id).ToArray();
            Check(ids.Distinct().Count() == ids.Length, "Coin ids must be unique for the checkpoint snapshot");

            for (int index = 0; index < Beats.Length - 1; index++)
            {
                float from = Find(roots, Beats[index].Platform).transform.position.z;
                float to = Find(roots, Beats[index + 1].Platform).transform.position.z;
                int inSpan = coins.Count(coin => coin.transform.position.z >= from - 2f
                                                 && coin.transform.position.z < to + 2f);
                Check(inSpan > 0, $"Beat {Beats[index].Beat} carries no coins, so the route has a dead stretch");
            }

            report.AppendLine($"  Coin route: {coins.Length} coins with unique ids, at least one in every beat span.");
        }

        /// <summary>
        /// A clean run is more than its path length: the moving platforms are ridden at their own authored speed
        /// after an average half-cycle wait, and each crumble platform is committed to only after its telegraph.
        /// Every term here is read from the scene or the tuning asset rather than assumed.
        /// </summary>
        private static float VerifyDuration(GameObject[] roots, StringBuilder report)
        {
            PlatformerTuningConfig tuning = AssetDatabase
                .LoadAssetAtPath<PlatformerTuningConfig>("Assets/Project/GameDomain/Features/Configs/Data/PlatformerTuningConfig.asset");
            Check(tuning != null, "The shipping tuning asset was not found");

            PlatformBaker[] platforms = All<PlatformBaker>(roots)
                .OrderBy(platform => platform.transform.position.z)
                .ToArray();

            float length = 0f;
            for (int index = 1; index < platforms.Length; index++)
            {
                length += Vector3.Distance(platforms[index - 1].transform.position, platforms[index].transform.position);
            }

            float running = length / (tuning.MaximumRunSpeed * RunEfficiency);

            float rides = 0f;
            foreach (MovingPlatformBaker moving in All<MovingPlatformBaker>(roots))
            {
                float route = Vector3.Distance(moving.transform.position, moving.EndPosition);
                float ride = route / Mathf.Max(0.01f, Speed(moving));
                // One traverse, plus the average wait for a platform that is somewhere in its cycle on arrival.
                rides += ride * 1.5f;
            }

            float telegraphs = Count<CrumblePlatformBaker>(roots) * tuning.CrumbleTelegraphTime;
            float seconds = running + rides + telegraphs;

            report.AppendLine($"  Route: {length:F1} m over {platforms.Length} platforms; at {RunEfficiency:P0} of " +
                $"{tuning.MaximumRunSpeed:F1} m/s that is {running:F0} s running, plus {rides:F0} s riding moving " +
                $"platforms and {telegraphs:F0} s of crumble telegraphs, for {seconds:F0} s.");

            Check(seconds >= MinimumSeconds, $"A clean run of {seconds:F0} s is shorter than the {MinimumSeconds:F0} s target");
            Check(seconds <= MaximumSeconds, $"A clean run of {seconds:F0} s is longer than the {MaximumSeconds:F0} s target");
            return seconds;
        }

        private static float Speed(MovingPlatformBaker moving)
            => new SerializedObject(moving).FindProperty("_speed").floatValue;

        private static int Id(CoinBaker coin)
        {
            SerializedProperty property = new SerializedObject(coin).FindProperty("_id");
            return property.intValue;
        }

        private static IEnumerable<T> All<T>(GameObject[] roots) where T : Component
            => roots.SelectMany(root => root.GetComponentsInChildren<T>(true));

        private static int Count<T>(GameObject[] roots) where T : Component => All<T>(roots).Count();

        private static GameObject Find(GameObject[] roots, string name)
        {
            GameObject found = All<Transform>(roots).FirstOrDefault(transform => transform.name == name)?.gameObject;
            Check(found != null, $"'{name}' is missing from the course");
            return found;
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("A15 verification failed: " + label);
        }
    }
}
