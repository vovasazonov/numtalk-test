using UnityEngine;

namespace Project.GameDomain.Features.Configs.Scripts
{
    [CreateAssetMenu(fileName = "PlatformerTuningConfig", menuName = "NumTalk/Platformer Tuning")]
    public sealed class PlatformerTuningConfig : ScriptableObject
    {
        [Header("Run (m/s, m/s²)")]
        [Min(0f)] public float MaximumRunSpeed = 7.5f;
        [Min(0f)] public float GroundAcceleration = 60f;
        [Min(0f)] public float GroundDeceleration = 70f;
        [Range(0f, 1f)] public float AirAccelerationScale = 0.55f;

        [Header("Jump")]
        [Tooltip("Target apex height in metres.")]
        [Min(0f)] public float TargetJumpApexHeight = 2.6f;
        [Tooltip("Upward gravity in m/s².")]
        [Min(0f)] public float AscentGravity = 24f;
        [Min(1f)] public float FallGravityMultiplier = 1.7f;
        [Range(0f, 1f)] public float EarlyReleaseVelocityCut = 0.45f;
        [Min(0f)] public float TerminalFallSpeed = 32f;

        [Header("Forgiveness (seconds)")]
        [Min(0f)] public float CoyoteTime = 0.11f;
        [Min(0f)] public float JumpBufferTime = 0.14f;

        [Header("Character contacts")]
        [Min(0.001f)] public float GroundProbeDistance = 0.12f;
        [Min(0f)] public float GroundStickSpeed = 2f;
        [Min(0f)] public float MaximumInheritedPlatformSpeed = 12f;
        public LayerMask GroundProbeMask = (1 << 9) | (1 << 10) | (1 << 11);

        [Header("Platforms")]
        [Range(0f, 1f)] public float IceDecelerationScale = 0.10f;
        [Min(0f)] public float CrumbleTelegraphTime = 0.35f;
        [Min(0f)] public float CrumbleFallDelay = 0.55f;
        [Min(0f)] public float CrumbleRespawnTime = 3f;

        [Header("Crate")]
        [Min(0f)] public float CrateMass = 6f;
        [Min(0f)] public float CratePushAcceleration = 18f;

        [Header("Combat")]
        [Min(0f)] public float KnockbackSpeed = 9f;
        [Min(0f)] public float AirborneKnockbackHalfLife = 0.28f;
        [Min(0f)] public float GroundedKnockbackHalfLife = 0.16f;
        [Min(0f)] public float ProjectileRadius = 0.18f;
        [Min(0f)] public float ProjectileLifeTime = 4f;
        [Tooltip("What a projectile sweep can hit. Enemy and EnemyProjectile are absent, so a shooter cannot hit itself or its own shots.")]
        public LayerMask ProjectileHitMask = (1 << 8) | (1 << 9) | (1 << 10) | (1 << 11);
        [Min(0f)] public float StompBounceSpeed = 6.5f;
        [Min(0f)] public float HeldJumpStompBounceSpeed = 8.25f;

        [Header("Camera")]
        [Tooltip("Camera position relative to the follow anchor, in metres. Course forward is +Z.")]
        public Vector3 CourseCameraOffset = new(0f, 8f, -11f);
        [Tooltip("Look target relative to the follow anchor, in metres. Looks ahead to the next landing.")]
        public Vector3 CourseCameraFocusOffset = new(0f, 1f, 3f);
        [Range(30f, 90f)] public float CourseCameraFieldOfView = 60f;
        [Tooltip("Maximum horizontal velocity lead, in metres.")]
        [Min(0f)] public float MaximumCameraLead = 2f;
        [Tooltip("Single-frame player displacement that snaps the camera after a respawn, in metres.")]
        [Min(1f)] public float CameraTeleportDistance = 12f;
        [Min(0f)] public float FollowDamping = 8f;
        [Min(0f)] public float VelocityLeadTime = 0.18f;
        [Min(0f)] public float AirborneVerticalDamping = 4f;

        [Header("Touch controls")]
        [Tooltip("Touches at or right of this fraction of screen width are jump; left of it drive the stick.")]
        [Range(0f, 1f)] public float JumpRegionScreenFraction = 0.5f;
        [Tooltip("Thumb travel from the anchored centre that reads as full deflection, in inches.")]
        [Min(0.01f)] public float StickMaximumRadiusInches = 0.32f;
        [Min(0f)] public float StickDeadZoneInches = 0.04f;

        [Header("Feedback")]
        [Min(0f)] public float LandingSquashDuration = 0.12f;
        [Min(0f)] public float PickupPopDuration = 0.18f;
    }
}