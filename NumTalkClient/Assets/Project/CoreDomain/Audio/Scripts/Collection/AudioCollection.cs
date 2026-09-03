using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Project.CoreDomain.Audio.Scripts.Configs.AudioPlayer;
using Project.CoreDomain.Audio.Scripts.Player;
using Project.CoreDomain.Audio.Scripts.Source;
using Project.CoreDomain.Content;
using Project.CoreDomain.Engine;
using Project.CoreDomain.Time;
using Project.CoreDomain.View;

namespace Project.CoreDomain.Audio.Scripts.Collection
{
    internal class AudioCollection : IAudioCollection, IDisposable
    {
        private readonly string _audioSourcePrefabGuid;
        private readonly IEngineService _engineService;
        private readonly ITimeService _timeService;
        private readonly IContentService _contentService;
        private readonly IViewService _viewService;
        private readonly HashSet<AudioPlayer> _players = new();
        private readonly List<AudioPlayer> _toRemove = new();
        private float _volume = 1f;
        private bool _isMuted;

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;

                foreach (var player in _players)
                {
                    player.Volume = _volume;
                }
            }
        }

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;

                foreach (var player in _players)
                {
                    player.IsMuted = _isMuted;
                }
            }
        }

        public AudioCollection(
            string audioSourcePrefabGuid,
            IEngineService engineService,
            ITimeService timeService,
            IContentService contentService,
            IViewService viewService
        )
        {
            _audioSourcePrefabGuid = audioSourcePrefabGuid;
            _engineService = engineService;
            _timeService = timeService;
            _contentService = contentService;
            _viewService = viewService;
            
            _engineService.Updating += OnUpdating;
        }

        public void Dispose()
        {
            _engineService.Updating -= OnUpdating;
        }

        private void OnUpdating()
        {
            foreach (var player in _players)
            {
                if (player.IsStopped)
                {
                    _toRemove.Add(player);
                }
                else
                {
                    player.Update();
                }
            }

            foreach (var player in _toRemove)
            {
                _players.Remove(player);
                player.Dispose();
            }

            _toRemove.Clear();
        }

        public async UniTask<IAudioStopper> Play(string playerId)
        {
            var configKeeper = await _contentService.LoadAsync<AudioPlayerConfig>(playerId);
            var audioSourceKeeper = await _viewService.CreateAsync<AudioSourceView>(_audioSourcePrefabGuid);
            var player = new AudioPlayer(configKeeper, audioSourceKeeper, _timeService);
            _players.Add(player);
            player.Volume = _volume;
            player.IsMuted = _isMuted;
            player.Play();
            return player;
        }

        public IAudioStopper PlayImmediately(string playerId)
        {
            var configKeeper = _contentService.LoadAsync<AudioPlayerConfig>(playerId).GetAwaiter().GetResult();
            var audioSourceKeeper = _viewService.CreateAsync<AudioSourceView>(_audioSourcePrefabGuid).GetAwaiter().GetResult();
            var player = new AudioPlayer(configKeeper, audioSourceKeeper, _timeService);
            _players.Add(player);
            player.Volume = _volume;
            player.IsMuted = _isMuted;
            player.Play();
            return player;
        }

        public IAudioStopper PlayImmediately(AudioPlayerConfig config)
        {
            return PlayImmediately((IAudioPlayerConfig)config);
        }

        public IAudioStopper PlayImmediately(IAudioPlayerConfig config)
        {
            var audioSourceKeeper = _viewService.CreateAsync<AudioSourceView>(_audioSourcePrefabGuid).GetAwaiter().GetResult();
            var player = new AudioPlayer(config, audioSourceKeeper, _timeService);
            _players.Add(player);
            player.Volume = _volume;
            player.IsMuted = _isMuted;
            player.Play();
            return player;
        }
    }
}