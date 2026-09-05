using Project.GameDomain.Features.Platforms.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    /// <summary>Scene-owned, bounded presentation particles. No colliders, gameplay timers or input handlers.</summary>
    public sealed class CourseEffects : MonoBehaviour
    {
        public static CourseEffects Instance { get; private set; }
        public ParticleSystem Particles;
        public Text WeatherLabel;
        private FlashFreezePhase _phase;
        private int _seconds = -1;

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

        public void ShowWeather(in FlashFreezeComponent freeze)
        {
            int seconds = Mathf.CeilToInt((freeze.Phase == FlashFreezePhase.Warning
                ? freeze.WarningSeconds : freeze.FrozenSeconds) - freeze.Elapsed);
            if (_phase == freeze.Phase && _seconds == seconds) return;
            _phase = freeze.Phase;
            _seconds = seconds;
            WeatherLabel.gameObject.SetActive(_phase == FlashFreezePhase.Warning || _phase == FlashFreezePhase.Frozen);
            WeatherLabel.color = _phase == FlashFreezePhase.Warning ? new Color(1f, 0.78f, 0.32f) : new Color(0.65f, 0.95f, 1f);
            WeatherLabel.text = _phase == FlashFreezePhase.Warning
                ? $"COLD FRONT IN {seconds}  /  WATCH YOUR FOOTING"
                : $"FLASH FREEZE  /  SLIPPERY FOR {seconds}s";
        }

        public void Clear()
        {
            Particles.Clear();
            WeatherLabel.gameObject.SetActive(false);
            _seconds = -1;
        }
    }
}
