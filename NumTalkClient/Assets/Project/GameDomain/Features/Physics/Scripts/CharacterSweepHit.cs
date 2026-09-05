using Arch.Core;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Physics.Scripts
{
    /// <summary>
    /// One swept-capsule hit, as plain data. <see cref="TopY"/> is the world height of the hit collider's top,
    /// which is what discriminates a stomp from a side hit without trusting the post-move contact normal.
    /// </summary>
    public struct CharacterSweepHit
    {
        public Entity Other;
        public float3 Normal;
        public float3 Point;
        public float TopY;
    }
}
