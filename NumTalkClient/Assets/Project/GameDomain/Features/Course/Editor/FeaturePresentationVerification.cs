using System;
using Arch.Core;
using Project.GameDomain.Features.Checkpoints.Scripts;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.Enemies.Scripts;
using Project.GameDomain.Features.Pickup.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Project.GameDomain.Features.Course.Editor
{
    /// <summary>Regression coverage for the feature split: serialized bindings, composition and pooled visual reset.</summary>
    public static class FeaturePresentationVerification
    {
        [MenuItem("NumTalk/Verify Feature Presentation")]
        public static void RunMenu() => Debug.Log(Run());

        public static string Run()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CourseVisualCatalog>(CourseVisualBuilder.CatalogPath);
            VerifyBindings(catalog);
            VerifyPlayer(catalog);
            VerifyPlatformComposition(catalog);
            VerifyOtherFeatures(catalog);
            VerifyCheckpointGate(catalog);
            return "Feature presentation passed: owning prefab bindings, player animation/landing and interpolation without " +
                "moving collision, frozen/crumble composition, pooled reset, shooter charge, coin rotation and checkpoint tint.";
        }

        private static void VerifyBindings(CourseVisualCatalog catalog)
        {
            foreach (var entry in catalog.Entries)
            {
                Check(AssetDatabase.GetAssetPath(entry.Prefab) == CourseVisualBuilder.ModelPath(entry.Model), "Wrong art owner: " + entry.Model);
                Type role = entry.Model switch
                {
                    CourseModel.Player => typeof(PlayerModelPresentation),
                    CourseModel.Patrol or CourseModel.Shooter => typeof(EnemyModelPresentation),
                    CourseModel.Coin => typeof(CoinModelPresentation),
                    CourseModel.Checkpoint => typeof(CheckpointModelPresentation),
                    CourseModel.Grass or CourseModel.Ice or CourseModel.Moving or CourseModel.Crumble => typeof(FlashFreezeModelPresentation),
                    _ => null,
                };
                if (role != null) Check(entry.Prefab.GetComponent(role) != null, "Missing feature view: " + entry.Model);
                if (entry.Model == CourseModel.Patrol || entry.Model == CourseModel.Shooter)
                    Check(entry.Prefab.GetComponent<EnemyModelPresentation>().IsPatrol == (entry.Model == CourseModel.Patrol), "Enemy role changed");
            }
        }

        private static ModelPresentationFrame Frame(Vector3 position, bool initialized = true) => new()
        {
            Position = position, PreviousPosition = position, DeltaTime = 1f / 60f, Time = 1.25f,
            Initialized = initialized, Tint = Color.white, Glow = Color.black, AnimationState = -1,
        };

        private static void VerifyPlayer(CourseVisualCatalog catalog)
        {
            var world = World.Create();
            var collision = new GameObject("PresentationVerificationCollision");
            var listener = new GameObject("VisualListener"); listener.transform.SetParent(collision.transform);
            var model = new GameObject("PlayerArt"); model.transform.SetParent(listener.transform);
            collision.transform.position = new Vector3(3000, 0, 0);
            Vector3 original = collision.transform.position;
            try
            {
                Entity entity = world.Create(new PlayerMotorComponent { HasSimulationPose = true, PreviousPosition = original, Velocity = new float3(2, -3, 0) },
                    new ShapeComponent { LocalOffset = new float3(0, 0.9f, 0) }, new GroundStateComponent(), new ExternalVelocityComponent());
                var feature = model.AddComponent<PlayerModelPresentation>();
                feature.Bind(world, entity, catalog.Tuning);
                var frame = Frame(original, false);
                feature.Present(ref frame);
                Check(frame.AnimationState == 3, "Falling animation changed");
                Check(Vector3.Distance(listener.transform.position, original + Vector3.up * 0.9f) < 0.001f, "Visual interpolation lost its offset");
                world.Get<GroundStateComponent>(entity).IsGrounded = true;
                frame = Frame(original);
                feature.Present(ref frame);
                Check(frame.AnimationState == 1 && model.transform.localScale.y < 1f, "Landing squash/walk selection changed");
                Check(collision.transform.position == original && collision.transform.localScale == Vector3.one, "Player visual moved collision");
                feature.ResetPresentation();
                world.Get<PlayerMotorComponent>(entity).Velocity = float3.zero;
                frame = Frame(original, false);
                feature.Present(ref frame);
                Check(model.transform.localScale == Vector3.one && frame.AnimationState == 0, "Pooled player retained its landing pulse");
            }
            finally { UnityEngine.Object.DestroyImmediate(collision); World.Destroy(world); }
        }

        private static void VerifyPlatformComposition(CourseVisualCatalog catalog)
        {
            var world = World.Create();
            var model = UnityEngine.Object.Instantiate(catalog.Find(CourseModel.Grass).Prefab);
            try
            {
                Entity entity = world.Create(new FlashFreezeComponent { Phase = FlashFreezePhase.Frozen },
                    new CrumbleStateComponent { Phase = CrumblePhase.Telegraphing, PhaseTimer = 0.1f });
                var crumble = model.GetComponent<CrumbleModelPresentation>();
                var freeze = model.GetComponent<FlashFreezeModelPresentation>();
                crumble.Bind(world, entity, catalog.Tuning); freeze.Bind(world, entity, catalog.Tuning);
                var frame = Frame(Vector3.zero);
                crumble.Present(ref frame); freeze.Present(ref frame);
                Check(frame.Glow.r > 0f && frame.Tint.b == 1f && frame.Tint.r < 1f, "Platform visual behaviors stopped composing");
                Check(model.transform.Find("FrostOverlay").gameObject.activeSelf, "Frozen overlay missing");
                freeze.ResetPresentation();
                Check(!model.transform.Find("FrostOverlay").gameObject.activeSelf, "Pooled frost was left active");
                Check(world.Get<FlashFreezeComponent>(entity).Phase == FlashFreezePhase.Frozen &&
                    world.Get<CrumbleStateComponent>(entity).PhaseTimer == 0.1f, "Presentation mutated platform state");
            }
            finally { UnityEngine.Object.DestroyImmediate(model); World.Destroy(world); }
        }

        private static void VerifyOtherFeatures(CourseVisualCatalog catalog)
        {
            var world = World.Create();
            var model = new GameObject("FeaturePresentationVerification");
            try
            {
                Entity shooter = world.Create(new ShooterComponent { FireDirection = math.forward(), WindUpTimer = 0.5f, WindUpTime = 1f });
                var enemy = model.AddComponent<EnemyModelPresentation>(); enemy.Bind(world, shooter, catalog.Tuning);
                var frame = Frame(Vector3.zero); enemy.Present(ref frame);
                Check(frame.AnimationState == 0 && frame.Glow.r > 0f && model.transform.localScale.y < 1f, "Shooter charge changed");
                Entity pickup = world.Create(new PickupComponent());
                var coin = model.AddComponent<CoinModelPresentation>(); coin.Bind(world, pickup, catalog.Tuning);
                frame = Frame(new Vector3(0, 0, 6)); coin.Present(ref frame);
                Check(Quaternion.Angle(model.transform.localRotation, Quaternion.identity) > 1f, "Coin stopped rotating");
                Entity marker = world.Create(new CheckpointComponent { IsActivated = true });
                var checkpoint = model.AddComponent<CheckpointModelPresentation>(); checkpoint.Bind(world, marker, catalog.Tuning);
                frame = Frame(Vector3.zero); checkpoint.Present(ref frame);
                Check(frame.Glow.g > frame.Glow.r, "Activated checkpoint tint changed");
            }
            finally { UnityEngine.Object.DestroyImmediate(model); World.Destroy(world); }
        }

        private static void VerifyCheckpointGate(CourseVisualCatalog catalog)
        {
            var world = World.Create();
            var model = UnityEngine.Object.Instantiate(catalog.Find(CourseModel.Checkpoint).Prefab);
            try
            {
                Entity marker = world.Create(new CheckpointComponent());
                var view = model.GetComponent<CheckpointModelPresentation>();
                view.Bind(world, marker, catalog.Tuning);
                var frame = Frame(new Vector3(10, 2, 5), false);
                view.Present(ref frame);
                Check(view.Gate.gameObject.activeSelf, "Unused checkpoint invitation missing");
                Check(Vector3.Distance(view.Gate.position, frame.Position + Vector3.up * 1.1f) < 0.001f, "Gate does not mark trigger center");
                world.Get<CheckpointComponent>(marker).IsActivated = true;
                view.Present(ref frame);
                Check(view.Gate.gameObject.activeSelf, "Activation should briefly keep the gate visible");
                frame.DeltaTime = 1.2f;
                view.Present(ref frame);
                Check(!view.Gate.gameObject.activeSelf, "Activated gate did not fade away");
                view.ResetPresentation();
                view.Present(ref frame);
                Check(!view.Gate.gameObject.activeSelf, "Pooling replayed checkpoint activation");
                world.Get<CheckpointComponent>(marker).IsActivated = false;
                view.Present(ref frame);
                Check(view.Gate.gameObject.activeSelf, "Run restart did not restore checkpoint invitation");
            }
            finally { UnityEngine.Object.DestroyImmediate(model); World.Destroy(world); }
        }

        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
