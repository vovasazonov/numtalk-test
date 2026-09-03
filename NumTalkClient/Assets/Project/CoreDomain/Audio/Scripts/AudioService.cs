using System;
using System.Collections.Generic;
using Project.CoreDomain.Audio.Scripts.Collection;
using Project.CoreDomain.Content;
using Project.CoreDomain.Engine;
using Project.CoreDomain.Time;
using Project.CoreDomain.View;

namespace Project.CoreDomain.Audio.Scripts
{
    public class AudioService : IAudioService, IDisposable
    {
        private readonly List<AudioCollection> _collections;

        public IAudioCollection Sound { get; }
        public IAudioCollection Music { get; }

        public AudioService(
            string audioSourcePrefabGuid,
            IEngineService engineService,
            ITimeService timeService,
            IContentService contentService,
            IViewService viewService
        )
        {
            _collections = new List<AudioCollection>(2);

            Sound = InitializeUpdateable(new AudioCollection(audioSourcePrefabGuid, engineService, timeService, contentService, viewService));
            Music = InitializeUpdateable(new AudioCollection(audioSourcePrefabGuid, engineService, timeService, contentService, viewService));
            Music.Volume = 0.7f;
        }

        private AudioCollection InitializeUpdateable(AudioCollection audioCollection)
        {
            _collections.Add(audioCollection);
            return audioCollection;
        }

        public void Dispose()
        {
            foreach (var audioCollection in _collections)
            {
                audioCollection.Dispose();
            }
            _collections.Clear();
        }
    }
}