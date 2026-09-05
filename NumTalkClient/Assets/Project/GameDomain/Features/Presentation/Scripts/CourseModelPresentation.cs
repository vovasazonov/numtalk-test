using Arch.Core;
using Project.GameDomain.Features.Configs.Scripts;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    /// <summary>Animates only the visual child. ECS and the collision root are never written here.</summary>
    public sealed class CourseModelPresentation : MonoBehaviour
    {
        private World _world;
        private Entity _entity;
        private PlatformerTuningConfig _tuning;
        private Renderer[] _renderers;
        private Color[] _baseColors;
        private MaterialPropertyBlock _properties;
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable[] _clips;
        private bool _bound, _initialized;
        private ModelPresentationFeature[] _features;
        private Vector3 _lastPosition;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");

        public void Bind(World world, Entity entity, CourseVisualCatalog catalog, CourseVisualCatalog.Entry entry)
        {
            _world = world; _entity = entity; _tuning = catalog.Tuning;
            _renderers ??= GetComponentsInChildren<Renderer>(true);
            if (_baseColors == null)
            {
                _baseColors = new Color[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++) _baseColors[i] = _renderers[i].sharedMaterial.HasProperty(BaseColor) ? _renderers[i].sharedMaterial.GetColor(BaseColor) : Color.white;
            }
            _properties ??= new MaterialPropertyBlock();
            _features ??= GetComponents<ModelPresentationFeature>();
            foreach (var feature in _features) feature.Bind(world, entity, _tuning);
            _bound = true;
            if (entry.Idle == null) return;
            var animator = GetComponentInChildren<Animator>();
            animator.applyRootMotion = false;
            _graph = PlayableGraph.Create("Course character");
            var output = AnimationPlayableOutput.Create(_graph, "Character", animator);
            _mixer = AnimationMixerPlayable.Create(_graph, 4);
            output.SetSourcePlayable(_mixer);
            _clips = new AnimationClipPlayable[4];
            var animations = new[] { entry.Idle, entry.Walk, entry.Jump, entry.Fall };
            for (int i = 0; i < 4; i++)
            {
                _clips[i] = AnimationClipPlayable.Create(_graph, animations[i]);
                _graph.Connect(_clips[i], 0, _mixer, i);
                _mixer.SetInputWeight(i, i == 0 ? 1f : 0f);
            }
            _graph.Play();
        }

        private void LateUpdate()
        {
            if (!_bound || !_world.IsAlive(_entity)) return;
            float dt = Time.deltaTime;
            Vector3 pose = _world.Get<EntityTransformComponent>(_entity).Position;
            bool teleported = _initialized && Vector3.Distance(pose, _lastPosition) > _tuning.CameraTeleportDistance;
            if (teleported)
            {
                _initialized = false;
                CourseEffects.Instance?.Clear();
            }
            var frame = new ModelPresentationFrame
            {
                Position = pose, PreviousPosition = _lastPosition,
                DeltaTime = dt, Time = Time.time, Initialized = _initialized, Teleported = teleported,
                Tint = Color.white, Glow = Color.black, AnimationState = -1,
            };
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            foreach (var feature in _features) feature.Present(ref frame);
            if (frame.AnimationState >= 0) Animate(frame.AnimationState, dt);

            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].GetPropertyBlock(_properties);
                _properties.SetColor(Emission, frame.Glow);
                _properties.SetColor(BaseColor, _baseColors[i] * frame.Tint);
                _renderers[i].SetPropertyBlock(_properties);
            }
            _lastPosition = pose;
            _initialized = true;
        }

        private void Animate(int state, float dt)
        {
            if (!_graph.IsValid()) return;
            float blend = 1f - Mathf.Exp(-18f * dt);
            for (int i = 0; i < 4; i++)
            {
                _mixer.SetInputWeight(i, Mathf.Lerp(_mixer.GetInputWeight(i), i == state ? 1f : 0f, blend));
                double length = _clips[i].GetAnimationClip().length;
                if (length > 0 && _clips[i].GetTime() >= length) _clips[i].SetTime(_clips[i].GetTime() % length);
            }
        }

        public void ReleaseFeedback()
        {
            if (!_bound || !_world.IsAlive(_entity)) return;
            foreach (var feature in _features) feature.ReleaseFeedback();
        }

        private void OnDisable()
        {
            if (_graph.IsValid()) _graph.Destroy();
            _bound = _initialized = false;
            if (_features != null) foreach (var feature in _features) feature.ResetPresentation();
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (_renderers != null) foreach (var renderer in _renderers) renderer.SetPropertyBlock(null);
        }
    }
}
