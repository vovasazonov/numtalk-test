using System;
using System.Linq;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.GameDomain.Features.Course.Editor
{
    public static class PolishVerification
    {
        [MenuItem("NumTalk/Verify Priority B")]
        public static void RunMenu() => Debug.Log(Run());

        public static string Run()
        {
            Project.GameDomain.Features.Platforms.Editor.FlashFreezeVerification.Run();
            VerifyArt();
            FeaturePresentationVerification.Run();
            return "Priority B editor checks passed: warning before traction change, timed thaw at 30/60/120 Hz, " +
                "moving/frozen surface composition, retained permanent ice, checkpoint and full-restart weather restore, " +
                "complete model catalog, collider-free art, and eight authored freeze surfaces. Device verification remains required.";
        }

        private static void VerifyArt()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CourseVisualCatalog>(CourseVisualBuilder.CatalogPath);
            Check(catalog != null && catalog.Entries.Length == 11, "Missing model catalog");
            foreach (var entry in catalog.Entries)
            {
                Check(entry.Prefab != null, "Missing art prefab: " + entry.Model);
                Check(entry.Prefab.GetComponentsInChildren<Collider>(true).Length == 0, "Art imported a collider: " + entry.Model);
                foreach (var renderer in entry.Prefab.GetComponentsInChildren<Renderer>(true))
                    Check(renderer.sharedMaterial != null && renderer.sharedMaterial.shader.isSupported, "Unsupported art material");
                if (entry.Model == CourseModel.Player || entry.Model == CourseModel.Patrol || entry.Model == CourseModel.Shooter)
                    Check(entry.Idle != null && entry.Walk != null && entry.Jump != null && entry.Fall != null, "Missing animation set");
            }
            Scene scene = SceneManager.GetSceneByPath(CourseVisualBuilder.ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(CourseVisualBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                Check(roots.SelectMany(r => r.GetComponentsInChildren<FlashFreezeBaker>()).Count() == 8, "Expected eight freeze surfaces");
                foreach (var art in roots.SelectMany(r => r.GetComponentsInChildren<CourseVisualBaker>()))
                    Check(art.transform.Find("CourseArt") != null, "Missing authored model preview on " + art.name);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
