using Project.GameDomain.Features.Audio.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Goal.Scripts
{
    /// <summary>One finite salute per goal activation. Pooled views never own the run's completion state.</summary>
    public sealed class GoalModelPresentation : ModelPresentationFeature
    {
        public ParticleSystem Fireworks;
        private bool _observed, _reached;
        private float _remaining, _untilBurst;
        private int _burst;
        public bool IsCelebrating => _remaining > 0f;

        public override void Present(ref ModelPresentationFrame frame)
        {
            if (!World.TryGet(Entity, out GoalComponent goal)) return;
            if (_observed && goal.IsReached && !_reached)
            {
                _remaining = 4.2f;
                _untilBurst = 0f;
                _burst = 0;
                CourseAudio.Instance?.Play(CourseSound.Finish);
            }
            if (!goal.IsReached) _remaining = 0f;
            if (_remaining > 0f && Fireworks != null)
            {
                _remaining = Mathf.Max(0, _remaining - frame.DeltaTime);
                _untilBurst -= frame.DeltaTime;
                if (_untilBurst <= 0f)
                {
                    _untilBurst = 0.65f;
                    Salute(frame.Position);
                }
            }
            if (goal.IsReached) frame.Glow = new Color(0.6f, 0.35f, 0.05f);
            _observed = true;
            _reached = goal.IsReached;
        }

        private void Salute(Vector3 goal)
        {
            if (!Fireworks.isPlaying) Fireworks.Play();
            var color = _burst % 3 == 0 ? new Color(1f, 0.78f, 0.2f) :
                _burst % 3 == 1 ? new Color(0.2f, 1f, 0.8f) : new Color(1f, 0.35f, 0.6f);
            Vector3 center = goal + new Vector3((_burst % 2 == 0 ? -1 : 1) * 2.4f, 3.8f + (_burst % 3) * 0.45f, 0.5f);
            for (int i = 0; i < 40; i++)
            {
                Vector3 direction = Random.onUnitSphere;
                Fireworks.Emit(new ParticleSystem.EmitParams
                {
                    position = center,
                    velocity = direction * Random.Range(1.5f, 3f) + Vector3.up * 0.7f,
                    startColor = color,
                    startSize = Random.Range(0.09f, 0.17f),
                    startLifetime = Random.Range(1.2f, 1.9f),
                }, 1);
            }
            _burst++;
        }

        public override void ResetPresentation()
        {
            _observed = _reached = false;
            _remaining = _untilBurst = 0f;
            _burst = 0;
            if (Fireworks != null) Fireworks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
