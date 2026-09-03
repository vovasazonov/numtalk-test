using UnityEngine;

namespace Project.CoreDomain.Audio.Scripts.Source
{
    public class AudioSourceView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.AudioSource _audioSource;

        public UnityEngine.AudioSource AudioSource => _audioSource;
    }
}