namespace Project.CoreDomain.Audio.Scripts.Fade
{
    internal interface IAudioFade
    {
        float PercentFade { get; }
        
        void Update();
    }
}