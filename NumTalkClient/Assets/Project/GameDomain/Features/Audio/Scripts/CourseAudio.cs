using UnityEngine;

namespace Project.GameDomain.Features.Audio.Scripts
{
    public enum CourseSound { Coin, Checkpoint, LifeLost, Finish }

    /// <summary>Arena-owned audio presentation. One loop and bounded one-shot cues; no gameplay state.</summary>
    public sealed class CourseAudio : MonoBehaviour
    {
        public static CourseAudio Instance { get; private set; }
        public AudioClip Music, Coin, Confirm, LifeLost;
        [Range(0f, 1f)] public float MusicVolume = 0.16f;
        [Range(0f, 1f)] public float EffectsVolume = 0.5f;
        private AudioSource _music, _effects;
        private readonly float[] _lastPlayed = { -10f, -10f, -10f, -10f };
        private float _duckUntil;

        private void Awake()
        {
            Instance = this;
            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false; _music.loop = true; _music.spatialBlend = 0f;
            _music.volume = 0f; _music.clip = Music;
            _effects = gameObject.AddComponent<AudioSource>();
            _effects.playOnAwake = false; _effects.spatialBlend = 0f; _effects.volume = EffectsVolume;
            if (Music != null) _music.Play();
        }

        private void Update()
        {
            float volume = Time.unscaledTime < _duckUntil ? MusicVolume * 0.35f : MusicVolume;
            _music.volume = Mathf.MoveTowards(_music.volume, volume, Time.unscaledDeltaTime * 0.25f);
        }

        public void Play(CourseSound cue)
        {
            // Overlapping coins/contact reports cannot stack a loud burst of identical sounds.
            int index = (int)cue;
            if (Time.unscaledTime - _lastPlayed[index] < 0.1f) return;
            _lastPlayed[index] = Time.unscaledTime;
            AudioClip clip = cue switch
            {
                CourseSound.Coin => Coin,
                CourseSound.LifeLost => LifeLost,
                _ => Confirm,
            };
            if (cue == CourseSound.LifeLost) _duckUntil = Time.unscaledTime + 2f;
            if (clip != null) _effects.PlayOneShot(clip, cue == CourseSound.Coin ? 0.65f : 1f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
