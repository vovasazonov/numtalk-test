using Project.CoreDomain.Audio.Scripts.Collection;

namespace Project.CoreDomain.Audio.Scripts
{
    public interface IAudioService
    {
        IAudioCollection Music { get; }
        IAudioCollection Sound { get; }
    }
}