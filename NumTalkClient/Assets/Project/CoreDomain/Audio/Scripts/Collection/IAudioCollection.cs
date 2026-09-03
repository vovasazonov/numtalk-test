using Cysharp.Threading.Tasks;
using Project.CoreDomain.Audio.Scripts.Configs.AudioPlayer;
using Project.CoreDomain.Audio.Scripts.Player;

namespace Project.CoreDomain.Audio.Scripts.Collection
{
    public interface IAudioCollection
    {
        float Volume { get; set; }
        bool IsMuted { get; set; }
        
        UniTask<IAudioStopper> Play(string playerId);
        IAudioStopper PlayImmediately(string playerId);
        IAudioStopper PlayImmediately(AudioPlayerConfig config);
        IAudioStopper PlayImmediately(IAudioPlayerConfig config);
    }
}