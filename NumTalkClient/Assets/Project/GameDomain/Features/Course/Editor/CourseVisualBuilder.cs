using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.Course.Scripts;
using Project.GameDomain.Features.Pickup.Scripts;
using Project.GameDomain.Features.Checkpoints.Scripts;
using Project.GameDomain.ScreensDomain.ArenaDomain.Features.Ui.Editor;
using Project.GameDomain.Features.Enemies.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Project.GameDomain.Features.Course.Editor
{
    /// <summary>Repeatable art authoring. Never changes a collider, platform pose, or route dimension.</summary>
    public static class CourseVisualBuilder
    {
        public const string ScenePath = "Assets/Project/GameDomain/ScreensDomain/ArenaDomain/Scenes/ArenaScene.unity";
        public const string Art = "Assets/Project/GameDomain/Features/Presentation/Art";
        public const string CatalogPath = "Assets/Project/GameDomain/Features/Course/Data/CourseVisualCatalog.asset";
        private const string Source = "Assets/kenney_platformer-kit/Models/FBX format/";
        private const string TuningPath = "Assets/Project/GameDomain/Features/Configs/Data/PlatformerTuningConfig.asset";
        private static Material _atlas, _ice, _amber, _dark, _gold;

        [MenuItem("NumTalk/Apply Kenney Visual Pass")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before authoring the course.");
            Directory.CreateDirectory(Art);
            AssetDatabase.Refresh();
            _atlas = Material("KenneyAtlas", Color.white);
            _atlas.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "Textures/colormap.png"));
            _atlas.SetFloat("_Cull", 0f); // The kit includes single-sided flag cloth.
            _ice = Material("Glacier", new Color(0.30f, 0.80f, 0.94f), 0.48f, "Platforms");
            _amber = Material("CrumbleAmber", new Color(1f, 0.56f, 0.16f), feature: "Platforms");
            _dark = Material("Basalt", new Color(0.16f, 0.28f, 0.34f));
            _gold = Material("GoalGold", new Color(1f, 0.76f, 0.15f));
            var tuning = AssetDatabase.LoadAssetAtPath<PlatformerTuningConfig>(TuningPath);
            tuning.CourseCameraOffset = new Vector3(0f, 10f, -13f);
            tuning.CourseCameraFocusOffset = new Vector3(0f, 1f, 4f);
            tuning.CourseCameraFieldOfView = 55f;
            tuning.FreezeWarningSeconds = 3f;
            tuning.FreezeDurationSeconds = 12f;
            EditorUtility.SetDirty(tuning);

            var catalog = AssetDatabase.LoadAssetAtPath<CourseVisualCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<CourseVisualCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.Tuning = tuning;
            var specs = new (CourseModel id, string file, Material material)[]
            {
                (CourseModel.Grass, "block-grass-large", _atlas),
                (CourseModel.Ice, "block-snow-large", _ice),
                (CourseModel.Moving, "block-moving", _atlas),
                (CourseModel.Crumble, "block-moving", _amber),
                (CourseModel.Crate, "crate", _atlas),
                (CourseModel.Player, "character-oobi", _atlas),
                (CourseModel.Patrol, "character-oodi", _atlas),
                (CourseModel.Shooter, "character-oozi", _atlas),
                (CourseModel.Coin, "coin-gold", _atlas),
                (CourseModel.Checkpoint, "flag", _atlas),
                (CourseModel.Goal, "door-large-open", _gold),
            };
            var entries = new List<CourseVisualCatalog.Entry>();
            foreach (var spec in specs)
            {
                var entry = new CourseVisualCatalog.Entry { Model = spec.id, Prefab = CreateModel(spec.id, spec.file, spec.material) };
                var clips = AssetDatabase.LoadAllAssetsAtPath(Source + spec.file + ".fbx").OfType<AnimationClip>().ToArray();
                entry.Idle = clips.FirstOrDefault(c => c.name == "idle");
                entry.Walk = clips.FirstOrDefault(c => c.name == "walk");
                entry.Jump = clips.FirstOrDefault(c => c.name == "jump");
                entry.Fall = clips.FirstOrDefault(c => c.name == "fall");
                entries.Add(entry);
            }
            catalog.Entries = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            string listenerPath = "Assets/Project/GameDomain/Features/Presentation/Prefabs/ShapeComponentListener.prefab";
            var listener = PrefabUtility.LoadPrefabContents(listenerPath);
            var serialized = new SerializedObject(listener.GetComponent<ShapeComponentListener>());
            serialized.FindProperty("_catalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(listener, listenerPath);
            PrefabUtility.UnloadPrefabContents(listener);

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            foreach (var shape in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShapeBaker>()).ToArray())
                Dress(shape, catalog, tuning);
            var lighting = scene.GetRootGameObjects().First(r => r.name == "LightingAndBackdrop");
            var old = lighting.transform.Find("KenneyBackdrop");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var backdrop = new GameObject("KenneyBackdrop");
            backdrop.transform.SetParent(lighting.transform, false);
            DressBackdrop(scene, backdrop.transform, catalog);
            if (lighting.GetComponent<CourseAtmosphere>() == null) lighting.AddComponent<CourseAtmosphere>();
            foreach (var light in lighting.GetComponentsInChildren<Light>())
            {
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.91f, 0.76f);
                light.intensity = 1.35f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.8f;
                light.shadowBias = 0.04f;
                light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            }
            BuildEffects(lighting.transform);
            ArenaHudBuilder.Build();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Kenney course authored: 11 model families, preserved collision, animated characters, scenery, feedback and flash-freeze surfaces.");
        }

        private static Material Material(string name, Color color, float smoothness = 0.1f, string feature = "Presentation")
        {
            string path = $"Assets/Project/GameDomain/Features/{feature}/Art/{name}.mat";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateModel(CourseModel id, string file, Material material)
        {
            var root = new GameObject(id.ToString());
            var mesh = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source + file + ".fbx"), root.transform);
            mesh.name = "KenneyMesh";
            var renderers = mesh.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) { bounds.Encapsulate(renderer.bounds); renderer.sharedMaterial = material; }
            Vector3 normalization = new(1f / bounds.size.x, 1f / bounds.size.y, 1f / bounds.size.z);
            if (id == CourseModel.Coin || id == CourseModel.Player || id == CourseModel.Patrol || id == CourseModel.Shooter)
                normalization = Vector3.one / bounds.size.y;
            mesh.transform.localScale = normalization;
            mesh.transform.localPosition = -Vector3.Scale(bounds.center, normalization);
            if (mesh.GetComponentInChildren<SkinnedMeshRenderer>() != null && mesh.GetComponentInChildren<Animator>() == null) mesh.AddComponent<Animator>();
            foreach (var collider in mesh.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            if (id == CourseModel.Grass)
            {
                var frost = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source + "block-snow-large.fbx"));
                frost.name = "FrostOverlay";
                Bounds frostBounds = frost.GetComponentInChildren<Renderer>().bounds;
                Vector3 frostScale = new Vector3(0.98f / frostBounds.size.x, 0.035f / frostBounds.size.y, 0.98f / frostBounds.size.z);
                frost.transform.SetParent(root.transform, false);
                frost.transform.localPosition = new Vector3(0, 0.495f, 0) - Vector3.Scale(frostBounds.center, frostScale);
                frost.transform.localScale = frostScale;
                foreach (var renderer in frost.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = _ice;
                frost.SetActive(false);
            }
            ConfigureFeatures(root, id);
            string path = ModelPath(id);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void Dress(ShapeBaker shape, CourseVisualCatalog catalog, PlatformerTuningConfig tuning)
        {
            var old = shape.transform.Find("CourseArt");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var source = shape.GetComponent<MeshFilter>();
            if (source == null) source = shape.GetComponentInChildren<MeshFilter>(true);
            if (source == null) return;
            var sourceData = new SerializedObject(shape);
            sourceData.FindProperty("_source").objectReferenceValue = source;
            sourceData.ApplyModifiedPropertiesWithoutUndo();
            Vector3 size = source.transform.lossyScale;
            Vector3 offset = Quaternion.Inverse(shape.transform.rotation) * (source.transform.position - shape.transform.position);
            CourseModel model = CourseModel.Grass;
            string name = shape.name;
            if (shape.GetComponent<PlayerBaker>() != null) { model = CourseModel.Player; size = new Vector3(1.35f, 1.8f, 1f); offset = Vector3.up * 0.9f; }
            else if (shape.GetComponent<ShooterBaker>() != null) { model = CourseModel.Shooter; size = new Vector3(1.25f, 1f, 1f); }
            else if (shape.GetComponent<EnemyBaker>() != null) { model = CourseModel.Patrol; size = new Vector3(1.25f, 1f, 1f); }
            else if (name.StartsWith("Coin_")) { model = CourseModel.Coin; size = Vector3.one * 0.6f; }
            else if (name.StartsWith("Crate_")) model = CourseModel.Crate;
            else if (name.StartsWith("Checkpoint_")) { model = CourseModel.Checkpoint; size = new Vector3(1.4f, 2.8f, 0.25f); offset = new Vector3(-1.5f, 1.4f, 0f); }
            else if (name.StartsWith("Goal_")) { model = CourseModel.Goal; size = new Vector3(3.1f, 3.8f, 0.8f); offset = Vector3.up * 1.9f; }
            else if (shape.GetComponent<CrumblePlatformBaker>() != null) model = CourseModel.Crumble;
            else if (shape.GetComponent<IceSurfaceBaker>() != null) model = CourseModel.Ice;
            else if (shape.GetComponent<MovingPlatformBaker>() != null) model = CourseModel.Moving;
            if (model == CourseModel.Grass || model == CourseModel.Ice)
            {
                offset.y -= 0.8f;
                size.y += 1.6f;
            }
            var art = shape.GetComponent<CourseVisualBaker>() ?? shape.gameObject.AddComponent<CourseVisualBaker>();
            art.Model = model; art.Size = size; art.Offset = offset;
            foreach (var renderer in shape.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            var preview = (GameObject)PrefabUtility.InstantiatePrefab(catalog.Find(model).Prefab, shape.gameObject.scene);
            preview.name = "CourseArt";
            preview.transform.SetPositionAndRotation(shape.transform.position + shape.transform.rotation * offset, shape.transform.rotation);
            preview.transform.localScale = size;
            preview.transform.SetParent(shape.transform, true);
            if (name.StartsWith("Platform_FreezeRun"))
            {
                var freeze = shape.GetComponent<FlashFreezeBaker>() ?? shape.gameObject.AddComponent<FlashFreezeBaker>();
                freeze.Tuning = tuning;
                // The safe checkpoint platform is reached before the warning starts.
                freeze.TriggerZ = 113.5f;
            }
            EditorUtility.SetDirty(shape.gameObject);
        }

        private static void DressBackdrop(Scene scene, Transform parent, CourseVisualCatalog catalog)
        {
            var platforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CourseVisualBaker>())
                .Where(b => b.Model == CourseModel.Grass && !b.name.Contains("FreezeRun")).ToArray();
            foreach (var platform in platforms)
            {
                float top = platform.transform.position.y + platform.Offset.y + platform.Size.y * 0.5f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 p = platform.transform.position + new Vector3(side * (platform.Size.x * 0.5f - 0.6f), 0, -platform.Size.z * 0.27f);
                    p.y = top;
                    Prop("tree-pine-small", parent, p, 1.5f, side * 30f);
                    Prop("flowers", parent, p + new Vector3(-side * 0.5f, 0, 0.8f), 1.1f, 0);
                }
                Prop("rocks", parent, platform.transform.position + Vector3.down * 2.8f, 2.1f, 20f);
            }
            for (int i = 0; i < 12; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 p = new(side * (22f + i % 3 * 4f) + 6f, -8f - i % 3, i * 17f - 5f);
                var island = (GameObject)PrefabUtility.InstantiatePrefab(catalog.Find(CourseModel.Grass).Prefab, scene);
                island.name = "DistantIsland_" + i;
                island.transform.SetParent(parent, false);
                island.transform.position = p;
                island.transform.localScale = new Vector3(10f, 5f, 9f);
                Prop("tree", parent, p + new Vector3(1f, 2.5f, 0f), 3f, i * 37f);
                Prop("tree-pine", parent, p + new Vector3(-2f, 2.5f, 2f), 2.2f, i * 20f);
            }
        }

        private static void Prop(string model, Transform parent, Vector3 position, float scale, float yaw)
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source + model + ".fbx"), parent);
            instance.name = "Scenery_" + model;
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            instance.transform.localScale = Vector3.one * scale;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = _atlas;
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
        }

        private static void BuildEffects(Transform parent)
        {
            var old = parent.Find("CourseEffects");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("CourseEffects"); root.transform.SetParent(parent, false);
            var effects = root.AddComponent<CourseEffects>();
            var particles = new GameObject("PooledSparks").AddComponent<ParticleSystem>();
            particles.transform.SetParent(root.transform, false);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false; main.loop = false; main.maxParticles = 256;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = 0.8f;
            var emission = particles.emission; emission.enabled = false;
            var shape = particles.shape; shape.enabled = false;
            var size = particles.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = _gold;
            effects.Particles = particles;
            var canvas = new GameObject("WeatherNotice", typeof(Canvas), typeof(CanvasScaler));
            canvas.transform.SetParent(root.transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 25;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            var notice = root.AddComponent<FlashFreezeNotice>();
            notice.WeatherLabel = ArenaHudBuilder.Label(canvas.transform, "Weather", "", new Vector2(0.5f, 1f), new Vector2(0, -86), new Vector2(850, 52), 25);
            notice.WeatherLabel.gameObject.SetActive(false);
        }

        public static string ModelPath(CourseModel model)
        {
            string feature = model switch
            {
                CourseModel.Player => "Player",
                CourseModel.Patrol or CourseModel.Shooter => "Enemies",
                CourseModel.Coin => "Pickup",
                CourseModel.Checkpoint => "Checkpoints",
                CourseModel.Goal => "Goal",
                CourseModel.Crate => "Pushables",
                _ => "Platforms",
            };
            return $"Assets/Project/GameDomain/Features/{feature}/Art/{model}.prefab";
        }

        public static void ConfigureFeatures(GameObject root, CourseModel model)
        {
            // Order matches the previous view: role animation, crumble, freeze, checkpoint tint.
            if (model == CourseModel.Player) Ensure<PlayerModelPresentation>(root);
            if (model == CourseModel.Patrol || model == CourseModel.Shooter)
                Ensure<EnemyModelPresentation>(root).IsPatrol = model == CourseModel.Patrol;
            if (model == CourseModel.Coin) Ensure<CoinModelPresentation>(root);
            if (model == CourseModel.Grass || model == CourseModel.Ice || model == CourseModel.Moving || model == CourseModel.Crumble)
            {
                Ensure<CrumbleModelPresentation>(root);
                Ensure<FlashFreezeModelPresentation>(root);
            }
            if (model == CourseModel.Checkpoint) Ensure<CheckpointModelPresentation>(root);
        }

        private static T Ensure<T>(GameObject root) where T : Component
        {
            var component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }

        [MenuItem("NumTalk/Update Feature Presentation Bindings")]
        public static void UpdateFeatureBindings()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            var catalog = AssetDatabase.LoadAssetAtPath<CourseVisualCatalog>(CatalogPath);
            foreach (var entry in catalog.Entries)
            {
                string path = AssetDatabase.GetAssetPath(entry.Prefab);
                var root = PrefabUtility.LoadPrefabContents(path);
                ConfigureFeatures(root, entry.Model);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (var effects in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CourseEffects>(true)))
                {
                    var notice = Ensure<FlashFreezeNotice>(effects.gameObject);
                    notice.WeatherLabel = effects.GetComponentInChildren<Text>(true);
                    EditorUtility.SetDirty(notice);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            AssetDatabase.SaveAssets();
            Debug.Log("Feature presentation bindings updated without regenerating course geometry or art.");
        }
    }
}
