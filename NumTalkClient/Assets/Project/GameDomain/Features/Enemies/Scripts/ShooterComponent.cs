using Unity.Mathematics;

namespace Project.GameDomain.Features.Enemies.Scripts
{
    /// <summary>Fires pooled projectiles along <see cref="FireDirection"/> once the player is inside range.</summary>
    public struct ShooterComponent
    {
        public float3 FireDirection;
        public float Range;

        /// <summary>Seconds between shots.</summary>
        public float FireInterval;

        /// <summary>Readable wind-up before each shot, in seconds.</summary>
        public float WindUpTime;

        /// <summary>Projectile travel speed in metres per second.</summary>
        public float ProjectileSpeed;

        public float Cooldown;
    }
}
