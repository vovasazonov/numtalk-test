namespace Project.GameDomain.Features.Player.Scripts
{
    public enum PlayerLifePhase { Alive, Dying, Respawning }

    public struct HealthComponent
    {
        public int Lives;
        public int MaximumLives;

        /// <summary>Raised by whatever hurt the player this tick and consumed by the respawn system.</summary>
        public int PendingDamage;
        public bool FellOutOfCourse;
        public PlayerLifePhase Phase;
        public float PhaseRemaining;
        public int RespawnVersion;
        public bool IsProtected => Phase != PlayerLifePhase.Alive;
    }
}
