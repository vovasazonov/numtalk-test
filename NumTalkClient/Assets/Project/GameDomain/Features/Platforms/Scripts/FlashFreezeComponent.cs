namespace Project.GameDomain.Features.Platforms.Scripts
{
    public enum FlashFreezePhase { Ready, Warning, Frozen, Thawed }

    /// <summary>Temporary surface behavior; original ice and all collision geometry remain independent.</summary>
    public struct FlashFreezeComponent
    {
        public FlashFreezePhase Phase;
        public float Elapsed;
        public float TriggerZ;
        public float WarningSeconds;
        public float FrozenSeconds;
        public float DecelerationScale;

        public void Step(float playerZ, float dt)
        {
            if (dt <= 0f || Phase == FlashFreezePhase.Thawed) return;
            if (Phase == FlashFreezePhase.Ready)
            {
                if (playerZ < TriggerZ) return;
                Phase = FlashFreezePhase.Warning;
                Elapsed = 0f;
                return;
            }
            Elapsed += dt;
            if (Phase == FlashFreezePhase.Warning && Elapsed >= WarningSeconds)
            {
                Elapsed -= WarningSeconds;
                Phase = FlashFreezePhase.Frozen;
            }
            if (Phase == FlashFreezePhase.Frozen && Elapsed >= FrozenSeconds)
            {
                Phase = FlashFreezePhase.Thawed;
                Elapsed = 0f;
            }
        }
    }
}
