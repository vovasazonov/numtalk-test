namespace Project.GameDomain.Features.Platforms.Scripts
{
    /// <summary>Stable -> Telegraphing -> Falling -> Respawning. All durations are in seconds.</summary>
    public struct CrumbleStateComponent
    {
        public CrumblePhase Phase;
        public float PhaseTimer;
        public float TelegraphTime;
        public float FallDelay;
        public float RespawnTime;
    }
}
