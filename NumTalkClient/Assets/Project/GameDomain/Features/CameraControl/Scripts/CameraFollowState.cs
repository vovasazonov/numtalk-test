using Project.GameDomain.Features.Configs.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.CameraControl.Scripts
{
    /// <summary>Presentation-only follow state. Never moves the player or its collision root.</summary>
    public struct CameraFollowState
    {
        public bool Initialized;
        public float GroundHeight;
        public float3 Anchor;
        public float3 PreviousPlayerPosition;

        public void Step(float3 playerPosition, float3 velocity, bool grounded, float dt, PlatformerTuningConfig tuning)
        {
            bool snap = !Initialized || math.distance(playerPosition, PreviousPlayerPosition) > tuning.CameraTeleportDistance;
            if (snap) GroundHeight = playerPosition.y;
            else if (grounded) GroundHeight = playerPosition.y;
            // Freeze the reference during ascent; follow a fall below the last landing rather than losing the player.
            else GroundHeight = math.min(GroundHeight, playerPosition.y);

            float3 lead = new float3(velocity.x, 0f, velocity.z) * tuning.VelocityLeadTime;
            lead *= math.min(1f, tuning.MaximumCameraLead / math.max(0.0001f, math.length(lead)));
            float3 target = new float3(playerPosition.x, GroundHeight, playerPosition.z) + lead;
            if (snap) Anchor = target;
            else
            {
                float horizontalBlend = 1f - math.exp(-tuning.FollowDamping * math.max(0f, dt));
                float verticalBlend = 1f - math.exp(-(grounded ? tuning.FollowDamping : tuning.AirborneVerticalDamping) * math.max(0f, dt));
                Anchor.x = math.lerp(Anchor.x, target.x, horizontalBlend);
                Anchor.z = math.lerp(Anchor.z, target.z, horizontalBlend);
                Anchor.y = math.lerp(Anchor.y, target.y, verticalBlend);
            }
            Initialized = true;
            PreviousPlayerPosition = playerPosition;
        }
    }
}
