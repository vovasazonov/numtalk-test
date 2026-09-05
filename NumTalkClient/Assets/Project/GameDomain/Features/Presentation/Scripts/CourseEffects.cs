using UnityEngine;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    /// <summary>Scene-owned, bounded presentation particles. No colliders, gameplay timers or input handlers.</summary>
    public sealed class CourseEffects : MonoBehaviour
    {
        public static CourseEffects Instance { get; private set; }
        public ParticleSystem Particles;
        public event System.Action Cleared;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Burst(Vector3 position, Color color, int count = 12)
        {
            if (!Particles.isPlaying) Particles.Play();
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = Random.onUnitSphere;
                direction.y = Mathf.Abs(direction.y) + 0.5f;
                Particles.Emit(new ParticleSystem.EmitParams
                {
                    position = position, velocity = direction * 2.2f, startColor = color,
                    startSize = Random.Range(0.07f, 0.16f), startLifetime = Random.Range(0.25f, 0.55f),
                }, 1);
            }
        }

        public void Clear()
        {
            Particles.Clear();
            Cleared?.Invoke();
        }
    }
}
