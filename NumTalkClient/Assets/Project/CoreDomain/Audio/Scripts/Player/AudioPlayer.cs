using System;
using Project.CoreDomain.Audio.Scripts.Configs.AudioPlayer;
using Project.CoreDomain.Audio.Scripts.Fade;
using Project.CoreDomain.Audio.Scripts.Source;
using Project.CoreDomain.Content;
using Project.CoreDomain.Time;
using Project.CoreDomain.View;
using UnityEngine;

namespace Project.CoreDomain.Audio.Scripts.Player
{
    internal class AudioPlayer : IAudioPlayer, IDisposable
    {
        private readonly IViewService _viewService;
        private readonly AudioFade _fade;
        private AudioSource _source;
        private float _originalVolume = 1f;
        private IContentKeeper<AudioSourceView> _audioSourceKeeper;

        public string Id { get; }
        public bool IsStopped => !_source.isPlaying;

        public float Volume
        {
            get => _originalVolume * (1f - _fade.PercentFade);
            set
            {
                _originalVolume = value;
                _source.volume = _originalVolume * (1f - _fade.PercentFade);
            }
        }

        public bool IsMuted
        {
            get => _source.mute;
            set => _source.mute = value;
        }

        public AudioPlayer(IContentKeeper<IAudioPlayerConfig> configKeeper, IContentKeeper<AudioSourceView> audioSourceKeeper, ITimeService time)
        {
            Id = configKeeper.Value.Id;
            _audioSourceKeeper = audioSourceKeeper;
            _source = audioSourceKeeper.Value.AudioSource;
            _source.clip = configKeeper.Value.Clip;
            _source.loop = configKeeper.Value.IsLoop;
            _fade = new AudioFade(configKeeper.Value.FadeSeconds, _source, time);
            configKeeper.Dispose();
        }
        
        public AudioPlayer(IAudioPlayerConfig config, IContentKeeper<AudioSourceView> audioSourceKeeper, ITimeService time)
        {
            Id = config.Id;
            _audioSourceKeeper = audioSourceKeeper;
            _source = audioSourceKeeper.Value.AudioSource;
            _source.clip = config.Clip;
            _source.loop = config.IsLoop;
            _fade = new AudioFade(config.FadeSeconds, _source, time);
        }

        public void Play()
        {
            _source.Play();
        }

        public void Stop()
        {
            if (_audioSourceKeeper != null)
            {
                _source.Stop();
            }
        }

        public void Update()
        {
            _fade.Update();
            Volume = _originalVolume;
        }

        public void Dispose()
        {
            _audioSourceKeeper?.Dispose();
            _audioSourceKeeper = null;
            _source = null;
        }
    }
}