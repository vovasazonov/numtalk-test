using Project.CoreDomain.Time;
using UnityEngine;

namespace Project.CoreDomain.Audio.Scripts.Fade
{
    internal class AudioFade : IAudioFade
    {
        private readonly float _fadeSeconds;
        private readonly AudioSource _audioSource;
        private readonly ITimeService _time;
        private float _currentSeconds;
        private float _startPercent;
        private float _targetPercent;
        private bool _isFading;
        private bool _isOutFade;
        private float _lastClipTime;
        private bool _isBeginPlay;

        public float PercentFade { get; private set; }

        public AudioFade(float fadeSeconds, AudioSource source, ITimeService time)
        {
            _fadeSeconds = fadeSeconds;
            _lastClipTime = source.clip.length;
            _audioSource = source;
            _time = time;
            if (fadeSeconds > 0f)
            {
                PercentFade = 1f;
            }
        }

        public void Update()
        {
            DetectBeginPlay();
            DetectStartInFade();
            DetectStartOutFade();
            UpdateFade();
        }

        private void DetectBeginPlay()
        {
            var time = _audioSource.time;

            if (_lastClipTime == 0 && time == 0)
            {
                _isBeginPlay = true;
            }
            else
            {
                _isBeginPlay = !(time >= _lastClipTime);
            }

            _lastClipTime = time;
        }

        private void DetectStartInFade()
        {
            if (_isBeginPlay)
            {
                if (_isFading)
                {
                    StopFade();
                }

                StartInFade();
            }
        }

        private void UpdateFade()
        {
            if (_isFading)
            {
                _currentSeconds += _time.DeltaTime;

                if (_currentSeconds < _fadeSeconds)
                {
                    PercentFade = Lerp(_startPercent, _targetPercent, _currentSeconds / _fadeSeconds);
                }
                else
                {
                    StopFade();
                }
            }
        }

        private void DetectStartOutFade()
        {
            var timeAudioToEnd = _audioSource.clip.length - _audioSource.time;
            var isTimeSoundLessFadeSeconds = timeAudioToEnd < _fadeSeconds;

            if (isTimeSoundLessFadeSeconds)
            {
                if (!_isOutFade)
                {
                    _isOutFade = true;

                    if (_isFading)
                    {
                        StopFade();
                    }

                    StartOutFade();
                }
            }
            else
            {
                if (_isOutFade)
                {
                    _isOutFade = false;
                }
            }
        }

        private void StartInFade()
        {
            _isFading = true;
            _currentSeconds = 0;
            _startPercent = 1;
            _targetPercent = 0;
        }

        private void StartOutFade()
        {
            _isFading = true;
            _currentSeconds = 0;
            _startPercent = 0;
            _targetPercent = 1;
        }

        private void StopFade()
        {
            _isFading = false;
            PercentFade = 0f;
        }

        private float Lerp(float start, float end, float interpolationBetweenStartEnd)
        {
            return start * (1 - interpolationBetweenStartEnd) + end * interpolationBetweenStartEnd;
        }
    }
}