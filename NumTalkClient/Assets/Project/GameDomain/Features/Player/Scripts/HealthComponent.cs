namespace Project.GameDomain.Features.Player.Scripts
{
    public struct HealthComponent
    {
        public int Lives;
        public int MaximumLives;

        /// <summary>Raised by whatever hurt the player this tick and consumed by the respawn system.</summary>
        public int PendingDamage;
    }
}
