using Arch.Core;
using Project.GameDomain.Features.Checkpoints.Scripts;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.Enemies.Scripts;
using Project.GameDomain.Features.Pickup.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Player.Scripts;
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
        private CourseVisualCatalog.Entry _entry;
        private Renderer[] _renderers;
        private Color[] _baseColors;
        private Transform _frost;
        private MaterialPropertyBlock _properties;
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable[] _clips;
        private bool _bound, _wasGrounded, _initialized;
        private float _previousVertical, _squashTime;
        private Vector3 _lastPosition;
        private Quaternion _facing = Quaternion.identity;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");

        public void Bind(World world, Entity entity, CourseVisualCatalog catalog, CourseVisualCatalog.Entry entry)
        {
            _world = world; _entity = entity; _tuning = catalog.Tuning; _entry = entry;
            _renderers ??= GetComponentsInChildren<Renderer>(true);
            if (_baseColors == null)
            {
                _baseColors = new Color[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++) _baseColors[i] = _renderers[i].sharedMaterial.GetColor(BaseColor);
            }
            _properties ??= new MaterialPropertyBlock();
            _frost = transform.Find("FrostOverlay");
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
            if (_initialized && Vector3.Distance(pose, _lastPosition) > _tuning.CameraTeleportDistance)
            {
                _squashTime = 0f;
                _initialized = false;
                CourseEffects.Instance?.Clear();
            }
            Color tint = Color.white;
            Color glow = Color.black;
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;

            if (_world.TryGet(_entity, out PlayerMotorComponent motor))
            {
                bool grounded = _world.Get<GroundStateComponent>(_entity).IsGrounded;
                if (_initialized && grounded && !_wasGrounded)
                {
                    _squashTime = _tuning.LandingSquashDuration;
                    CourseEffects.Instance?.Burst(pose, new Color(0.85f, 1f, 0.9f), 8);
                }
                bool bounce = _initialized && !grounded && !_wasGrounded && _previousVertical < -0.5f && motor.Velocity.y > 4f;
                if (bounce) _squashTime = _tuning.LandingSquashDuration;
                _squashTime = Mathf.Max(0, _squashTime - dt);
                float pulse = Mathf.Sin(Mathf.PI * _squashTime / Mathf.Max(0.001f, _tuning.LandingSquashDuration));
                float stretch = grounded ? -0.2f * pulse : Mathf.Clamp(motor.Velocity.y * 0.012f, -0.08f, 0.12f) + 0.16f * pulse;
                transform.localScale = new Vector3(1f - stretch * 0.5f, 1f + stretch, 1f - stretch * 0.5f);
                // Normalized model feet are at -0.5: compensate so squash remains anchored at the soles.
                transform.localPosition = new Vector3(0f, stretch * 0.5f, 0f);
                Vector3 horizontal = new Vector3(motor.Velocity.x, 0f, motor.Velocity.z);
                if (horizontal.sqrMagnitude > 0.04f)
                    _facing = Quaternion.Slerp(_facing, Quaternion.LookRotation(horizontal), 1f - Mathf.Exp(-16f * dt));
                transform.rotation = _facing;
                Animate(grounded ? (horizontal.magnitude > 0.25f ? 1 : 0) : (motor.Velocity.y > 0f ? 2 : 3), dt);
                if (_world.TryGet(_entity, out ExternalVelocityComponent external))
                {
                    float impact = Mathf.Clamp01(((Vector3)external.Velocity).magnitude / Mathf.Max(1f, _tuning.KnockbackSpeed));
                    glow = new Color(1f, 0.19f, 0.05f) * impact * 0.7f;
                    transform.rotation *= Quaternion.Euler(0f, 0f, -impact * 12f);
                }
                _wasGrounded = grounded;
                _previousVertical = motor.Velocity.y;
            }
            else if (_entry.Model == CourseModel.Patrol || _entry.Model == CourseModel.Shooter)
            {
                Vector3 travel = pose - _lastPosition; travel.y = 0f;
                if (_initialized && travel.sqrMagnitude > 0.00001f) _facing = Quaternion.LookRotation(travel);
                if (_world.TryGet(_entity, out ShooterComponent shooter))
                {
                    _facing = Quaternion.LookRotation(shooter.FireDirection);
                    float windup = shooter.WindUpTimer > 0f ? 1f - shooter.WindUpTimer / Mathf.Max(0.01f, shooter.WindUpTime) : 0f;
                    glow = new Color(1f, 0.1f, 0.02f) * windup;
                    transform.localScale = new Vector3(1f + windup * 0.15f, 1f - windup * 0.12f, 1f + windup * 0.15f);
                }
                transform.rotation = _facing;
                Animate(_entry.Model == CourseModel.Patrol ? 1 : 0, dt);
            }
            else if (_entry.Model == CourseModel.Coin)
            {
                transform.localRotation = Quaternion.Euler(0f, Time.time * 100f, 0f);
                transform.localPosition = Vector3.up * (Mathf.Sin(Time.time * 3f + pose.z) * 0.13f);
            }

            if (_world.TryGet(_entity, out CrumbleStateComponent crumble) && crumble.Phase == CrumblePhase.Telegraphing)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(crumble.PhaseTimer * 32f);
                glow = new Color(1f, 0.36f, 0.015f) * pulse;
                transform.localPosition = new Vector3(Mathf.Sin(crumble.PhaseTimer * 70f) * 0.012f, 0f, 0f);
            }
            if (_world.TryGet(_entity, out FlashFreezeComponent freeze))
            {
                CourseEffects.Instance?.ShowWeather(freeze);
                bool frozen = freeze.Phase == FlashFreezePhase.Frozen;
                if (_frost != null) _frost.gameObject.SetActive(frozen);
                if (freeze.Phase == FlashFreezePhase.Warning)
                    glow = new Color(0.15f, 0.65f, 1f) * (0.35f + 0.3f * Mathf.Sin(freeze.Elapsed * 8f));
                if (frozen) tint = new Color(0.64f, 0.88f, 1f);
            }
            if (_world.TryGet(_entity, out CheckpointComponent checkpoint) && checkpoint.IsActivated)
                glow = new Color(0.1f, 0.65f, 0.35f);

            _properties.SetColor(Emission, glow);
            for (int i = 0; i < _renderers.Length; i++)
            {
                _properties.SetColor(BaseColor, _baseColors[i] * tint);
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
            if (_world.TryGet(_entity, out PickupComponent pickup) && pickup.IsCollected)
                CourseEffects.Instance?.Burst(transform.position, new Color(1f, 0.76f, 0.12f), 16);
            if (_world.TryGet(_entity, out StompTargetComponent stomp) && stomp.IsDefeated)
                CourseEffects.Instance?.Burst(transform.position, new Color(1f, 0.55f, 0.28f), 20);
        }

        private void OnDisable()
        {
            if (_graph.IsValid()) _graph.Destroy();
            _bound = _initialized = _wasGrounded = false;
            _squashTime = _previousVertical = 0f;
            _facing = Quaternion.identity;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (_frost != null) _frost.gameObject.SetActive(false);
            if (_renderers != null) foreach (var renderer in _renderers) renderer.SetPropertyBlock(null);
        }
    }
}
