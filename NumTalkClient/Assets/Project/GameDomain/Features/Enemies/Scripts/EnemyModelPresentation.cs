using Project.GameDomain.Features.Presentation.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Enemies.Scripts
{
    public sealed class EnemyModelPresentation : ModelPresentationFeature
    {
        public bool IsPatrol;
        private Quaternion _facing = Quaternion.identity;

        public override void Present(ref ModelPresentationFrame frame)
        {
            Vector3 travel = frame.Position - frame.PreviousPosition; travel.y = 0f;
            if (frame.Initialized && travel.sqrMagnitude > 0.00001f) _facing = Quaternion.LookRotation(travel);
            if (World.TryGet(Entity, out ShooterComponent shooter))
            {
                _facing = Quaternion.LookRotation(shooter.FireDirection);
                float windup = shooter.WindUpTimer > 0f ? 1f - shooter.WindUpTimer / Mathf.Max(0.01f, shooter.WindUpTime) : 0f;
                frame.Glow = new Color(1f, 0.1f, 0.02f) * windup;
                transform.localScale = new Vector3(1f + windup * 0.15f, 1f - windup * 0.12f, 1f + windup * 0.15f);
            }
            transform.rotation = _facing;
            frame.AnimationState = IsPatrol ? 1 : 0;
        }

        public override void ReleaseFeedback()
        {
            if (World.TryGet(Entity, out StompTargetComponent stomp) && stomp.IsDefeated)
                CourseEffects.Instance?.Burst(transform.position, new Color(1f, 0.55f, 0.28f), 20);
        }

        public override void ResetPresentation() => _facing = Quaternion.identity;
    }
}
