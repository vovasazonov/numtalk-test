using UnityEngine;

namespace Project.CoreDomain.Audio.Scripts.Configs.AudioPlayer
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Audio/AudioPlayerConfig", order = 0)]
    public class AudioPlayerConfig : ScriptableObject, IAudioPlayerConfig
    {
        [SerializeField] private string _id;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private float _fadeSeconds;
        [SerializeField] private bool _isLoop;

        public string Id => _id;
        public AudioClip Clip => _clip;
        public float FadeSeconds => _fadeSeconds;
        public bool IsLoop => _isLoop;
    }
}