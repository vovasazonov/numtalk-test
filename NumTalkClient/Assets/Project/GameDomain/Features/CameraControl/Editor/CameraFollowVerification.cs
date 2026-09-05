using System;
using Project.GameDomain.Features.CameraControl.Scripts;
using Project.GameDomain.Features.Configs.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Project.GameDomain.Features.CameraControl.Editor
{
    public static class CameraFollowVerification
    {
        [MenuItem("NumTalk/Verify Course Camera")]
        public static void RunMenu() => Debug.Log(Run());

        public static string Run()
        {
            var tuning = ScriptableObject.CreateInstance<PlatformerTuningConfig>();
            var cameraObject = new GameObject("CameraVerification");
            try
            {
                var state = new CameraFollowState();
                state.Step(float3.zero, float3.zero, true, 1f / 60f, tuning);
                for (int i = 0; i < 60; i++)
                    state.Step(new float3(0, math.sin(i / 60f * math.PI) * 2.6f, 0), float3.zero, false, 1f / 60f, tuning);
                Check(math.abs(state.Anchor.y) < 0.0001f, "Jump does not lift camera reference");
                state.Step(new float3(0, 1, 0), float3.zero, true, 1f / 60f, tuning);
                Check(state.Anchor.y > 0 && state.Anchor.y < 1, "Raised landing blends smoothly");
                state.Step(new float3(0, -3, 0), float3.zero, false, 1f / 60f, tuning);
                Check(state.Anchor.y < 0 && state.Anchor.y > -3, "Falling below ground reference follows downward");
                state.Step(new float3(50, 5, 50), float3.zero, true, 1f / 60f, tuning);
                Check(math.distance(state.Anchor, new float3(50, 5, 50)) < 0.001f, "Teleport snaps");
                state = default;
                state.Step(float3.zero, new float3(100, 100, 0), true, 1f / 60f, tuning);
                Check(math.abs(state.Anchor.x - tuning.MaximumCameraLead) < 0.001f && state.Anchor.y == 0,
                    "Lead is horizontal and bounded");
                float3 baseline = SettledTarget(30, tuning);
                Check(math.distance(baseline, SettledTarget(60, tuning)) < 0.0001f &&
                    math.distance(baseline, SettledTarget(120, tuning)) < 0.0001f, "Exponential damping at 30/60/120 FPS");
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.fieldOfView = tuning.CourseCameraFieldOfView;
                camera.transform.SetPositionAndRotation(tuning.CourseCameraOffset,
                    Quaternion.LookRotation(tuning.CourseCameraFocusOffset - tuning.CourseCameraOffset));
                foreach (float aspect in new[] { 16f / 9f, 19.5f / 9f })
                {
                    camera.aspect = aspect;
                    Visible(camera, new Vector3(0, 0, 0));
                    Visible(camera, new Vector3(0, 4.6f, 0));
                    Visible(camera, new Vector3(-4, 0, 6));
                    Visible(camera, new Vector3(4, 0, 12));
                }
                return "A7 passed: frozen jump reference, landing and fall damping, teleport snap, bounded horizontal lead, 30/60/120 FPS damping, and player/apex/next-landing projection at 16:9 and 19.5:9.";
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(tuning);
            }
        }

        private static float3 SettledTarget(int fps, PlatformerTuningConfig tuning)
        {
            var state = new CameraFollowState();
            state.Step(float3.zero, float3.zero, true, 0f, tuning);
            for (int i = 0; i < fps; i++) state.Step(new float3(3, 1, 4), new float3(2, 0, 1), true, 1f / fps, tuning);
            return state.Anchor;
        }

        private static void Visible(Camera camera, Vector3 point)
        {
            Vector3 viewport = camera.WorldToViewportPoint(point);
            Check(viewport.z > 0 && viewport.x > 0.05f && viewport.x < 0.95f && viewport.y > 0.05f && viewport.y < 0.95f,
                "Safe framing of " + point + " at aspect " + camera.aspect + ": " + viewport);
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("A7 verification failed: " + label);
        }
    }
}
