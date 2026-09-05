using Project.GameDomain.Features.Audio.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Player.Scripts
{
    public sealed class PlayerModelPresentation : ModelPresentationFeature
    {
        private bool _wasGrounded;
        private PlayerLifePhase _lastLifePhase;
        private int _respawnVersion;
        private float _previousVertical, _squashTime;
        private Quaternion _facing = Quaternion.identity;

        public override void Present(ref ModelPresentationFrame frame)
        {
            if (!World.TryGet(Entity, out PlayerMotorComponent motor)) return;
            float dt = frame.DeltaTime;
            if (frame.Teleported) _squashTime = 0f;
            if (motor.HasSimulationPose)
            {
                var shape = World.Get<ShapeComponent>(Entity);
                float alpha = Mathf.Clamp01((float)((Time.timeAsDouble - Time.fixedTimeAsDouble) / Time.fixedDeltaTime));
                // The parent is the visual listener, not the collision/entity root.
                transform.parent.position = Vector3.Lerp(motor.PreviousPosition, frame.Position, alpha)
                    + transform.parent.parent.TransformVector(shape.LocalOffset);
            }
            if (World.TryGet(Entity, out HealthComponent life))
            {
                if (life.Phase == PlayerLifePhase.Dying)
                {
                    if (_lastLifePhase != PlayerLifePhase.Dying)
                    {
                        CourseEffects.Instance?.Burst(frame.Position + Vector3.up, new Color(1f, 0.3f, 0.2f), 20);
                        CourseAudio.Instance?.Play(CourseSound.LifeLost);
                    }
                    float progress = Mathf.Clamp01(1f - life.PhaseRemaining / Tuning.DeathDuration);
                    transform.rotation = _facing * Quaternion.Euler(0f, progress * 100f, Mathf.SmoothStep(0, 85, progress * 2));
                    transform.localPosition = Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 0.45f - progress * 0.25f);
                    transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.65f, progress);
                    frame.AnimationState = 3;
                    frame.Tint = Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(0.5f, 0.55f, 0.7f), progress);
                    _lastLifePhase = life.Phase;
                    return;
                }
                if (life.RespawnVersion != _respawnVersion)
                {
                    CourseEffects.Instance?.Burst(frame.Position + Vector3.up * 0.5f, new Color(0.4f, 1f, 1f), 28);
                    _respawnVersion = life.RespawnVersion;
                }
                if (life.Phase == PlayerLifePhase.Respawning)
                {
                    frame.Visible = Mathf.FloorToInt(life.PhaseRemaining * 9f) % 2 == 0;
                    frame.Tint = new Color(0.6f, 1f, 1f);
                }
                _lastLifePhase = life.Phase;
            }
            bool grounded = World.Get<GroundStateComponent>(Entity).IsGrounded;
            if (frame.Initialized && grounded && !_wasGrounded)
            {
                _squashTime = Tuning.LandingSquashDuration;
                CourseEffects.Instance?.Burst(frame.Position, new Color(0.85f, 1f, 0.9f), 8);
            }
            bool bounce = frame.Initialized && !grounded && !_wasGrounded && _previousVertical < -0.5f && motor.Velocity.y > 4f;
            if (bounce) _squashTime = Tuning.LandingSquashDuration;
            _squashTime = Mathf.Max(0, _squashTime - dt);
            float pulse = Mathf.Sin(Mathf.PI * _squashTime / Mathf.Max(0.001f, Tuning.LandingSquashDuration));
            float stretch = grounded ? -0.2f * pulse : Mathf.Clamp(motor.Velocity.y * 0.012f, -0.08f, 0.12f) + 0.16f * pulse;
            transform.localScale = new Vector3(1f - stretch * 0.5f, 1f + stretch, 1f - stretch * 0.5f);
            // Normalized model feet are at -0.5: compensate so squash remains anchored at the soles.
            transform.localPosition = new Vector3(0f, stretch * 0.5f, 0f);
            Vector3 horizontal = new Vector3(motor.Velocity.x, 0f, motor.Velocity.z);
            if (horizontal.sqrMagnitude > 0.04f)
                _facing = Quaternion.Slerp(_facing, Quaternion.LookRotation(horizontal), 1f - Mathf.Exp(-16f * dt));
            transform.rotation = _facing;
            frame.AnimationState = grounded ? (horizontal.magnitude > 0.25f ? 1 : 0) : (motor.Velocity.y > 0f ? 2 : 3);
            if (World.TryGet(Entity, out ExternalVelocityComponent external))
            {
                float impact = Mathf.Clamp01(((Vector3)external.Velocity).magnitude / Mathf.Max(1f, Tuning.KnockbackSpeed));
                frame.Glow = new Color(1f, 0.19f, 0.05f) * impact * 0.7f;
                transform.rotation *= Quaternion.Euler(0f, 0f, -impact * 12f);
            }
            _wasGrounded = grounded;
            _previousVertical = motor.Velocity.y;
        }

        public override void ResetPresentation()
        {
            _wasGrounded = false;
            _lastLifePhase = PlayerLifePhase.Alive;
            _respawnVersion = 0;
            _previousVertical = _squashTime = 0f;
            _facing = Quaternion.identity;
        }
    }
}
