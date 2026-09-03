namespace Project.CoreDomain.Audio.Scripts.Player
{
    public interface IAudioStopper
    {
        bool IsStopped { get; }
        void Stop();
    }
}