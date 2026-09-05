using Arch.Core;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Physics.Scripts
{
    /// <summary>One controller contact, as plain data. The colliding Unity object never reaches a system.</summary>
    public struct CharacterContact
    {
        public Entity Other;
        public float3 Normal;
        public float3 Point;
    }
}
