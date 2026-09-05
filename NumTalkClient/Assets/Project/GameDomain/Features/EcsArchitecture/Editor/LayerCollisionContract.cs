using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Project.GameDomain.Features.Configs.Scripts;
using UnityEditor;
using UnityEngine;

namespace Project.GameDomain.Features.EcsArchitecture.Editor
{
    /// <summary>
    /// A9: the single source of truth for the layer collision matrix. Apply writes it into Physics settings, Verify
    /// proves the project still matches. Gameplay layers collide with exactly what is listed here and nothing else,
    /// so an unlayered decoration can never become collision geometry.
    /// </summary>
    public static class LayerCollisionContract
    {
        /// <summary>Each gameplay layer and the complete set it is allowed to collide with. Symmetric by construction.</summary>
        private static readonly (string Layer, string[] CollidesWith)[] Contract =
        {
            ("Player", new[] { "Ground", "Platform", "Pushable", "Enemy", "EnemyProjectile", "Pickup", "KillZone" }),
            ("Ground", new[] { "Player", "Pushable", "Enemy", "EnemyProjectile", "CameraProbe" }),
            ("Platform", new[] { "Player", "Pushable", "Enemy", "EnemyProjectile", "CameraProbe" }),
            ("Pushable", new[] { "Player", "Ground", "Platform", "Enemy", "EnemyProjectile" }),
            ("Enemy", new[] { "Player", "Ground", "Platform", "Pushable" }),
            ("EnemyProjectile", new[] { "Player", "Ground", "Platform", "Pushable" }),
            ("Pickup", new[] { "Player" }),
            ("KillZone", new[] { "Player" }),
            ("CameraProbe", new[] { "Ground", "Platform" }),
        };

        [MenuItem("NumTalk/Apply Layer Contract")]
        public static void ApplyMenu()
        {
            Apply();
            Debug.Log("A9 applied: " + Run());
        }

        [MenuItem("NumTalk/Verify Layer Contract")]
        public static void RunMenu() => Debug.Log(Run());

        private static void Apply()
        {
            Dictionary<string, int> layers = ResolveLayers();
            foreach (int a in layers.Values)
            foreach (int b in Enumerable.Range(0, 32))
                UnityEngine.Physics.IgnoreLayerCollision(a, b, !ShouldCollide(layers, a, b));
            AssetDatabase.SaveAssets();
        }

        public static string Run()
        {
            Dictionary<string, int> layers = ResolveLayers();
            var report = new StringBuilder();
            report.AppendLine("A9 passed: the layer collision matrix matches the contract.");
            int allowed = 0;
            foreach (int a in layers.Values)
            foreach (int b in Enumerable.Range(0, 32))
            {
                bool expected = ShouldCollide(layers, a, b);
                bool actual = !UnityEngine.Physics.GetIgnoreLayerCollision(a, b);
                Check(expected == actual, $"{LayerMask.LayerToName(a)} vs {Describe(b)} should " +
                    (expected ? "collide" : "not collide") + " but does" + (actual ? "" : " not"));
                if (expected && a <= b) allowed++;
            }
            report.AppendLine($"  {allowed} allowed pairs across {layers.Count} gameplay layers; every other pair is ignored.");

            foreach ((string layer, string[] collidesWith) in Contract)
                report.AppendLine($"  {layer} ({layers[layer]}): {string.Join(", ", collidesWith)}");

            PlatformerTuningConfig tuning = LoadTuning();
            int standable = (1 << layers["Ground"]) | (1 << layers["Platform"]) | (1 << layers["Pushable"]);
            Check(tuning.GroundProbeMask.value == standable,
                $"GroundProbeMask is {tuning.GroundProbeMask.value} but the standable layers are {standable}");
            report.AppendLine("  GroundProbeMask is an explicit Ground|Platform|Pushable mask, matching the standable surfaces.");
            return report.ToString();
        }

        /// <summary>Symmetric lookup. A pair collides only if the contract names it from at least one side.</summary>
        private static bool ShouldCollide(Dictionary<string, int> layers, int a, int b)
        {
            foreach ((string layer, string[] collidesWith) in Contract)
            {
                if (layers[layer] != a && layers[layer] != b) continue;
                int other = layers[layer] == a ? b : a;
                if (collidesWith.Any(name => layers[name] == other)) return true;
            }
            return false;
        }

        private static Dictionary<string, int> ResolveLayers()
        {
            var layers = new Dictionary<string, int>();
            foreach (string name in Contract.Select(entry => entry.Layer))
            {
                int index = LayerMask.NameToLayer(name);
                Check(index >= 0, $"Layer '{name}' does not exist; the bakers resolve it by name");
                layers[name] = index;
            }
            return layers;
        }

        private static string Describe(int layer)
        {
            string name = LayerMask.LayerToName(layer);
            return string.IsNullOrEmpty(name) ? "layer " + layer : name;
        }

        private static PlatformerTuningConfig LoadTuning()
        {
            string guid = AssetDatabase.FindAssets("t:" + nameof(PlatformerTuningConfig)).FirstOrDefault();
            Check(guid != null, "No PlatformerTuningConfig asset exists");
            return AssetDatabase.LoadAssetAtPath<PlatformerTuningConfig>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("A9 verification failed: " + label);
        }
    }
}
