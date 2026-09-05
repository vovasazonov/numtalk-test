using Project.GameDomain.Features.Presentation.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    [RequireComponent(typeof(CourseEffects))]
    public sealed class FlashFreezeNotice : MonoBehaviour
    {
        public static FlashFreezeNotice Instance { get; private set; }
        public Text WeatherLabel;
        private CourseEffects _effects;
        private FlashFreezePhase _phase;
        private int _seconds = -1;
        private bool _runComplete;

        private void OnEnable()
        {
            Instance = this;
            _effects = GetComponent<CourseEffects>();
            _effects.Cleared += Clear;
        }

        private void OnDisable()
        {
            _effects.Cleared -= Clear;
            if (Instance == this) Instance = null;
        }

        public void SetRunComplete(bool complete)
        {
            _runComplete = complete;
            if (complete) WeatherLabel.gameObject.SetActive(false);
            else _seconds = -1;
        }

        public void ShowWeather(in FlashFreezeComponent freeze)
        {
            if (_runComplete) return;
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
            WeatherLabel.gameObject.SetActive(false);
            _seconds = -1;
        }
    }
}
