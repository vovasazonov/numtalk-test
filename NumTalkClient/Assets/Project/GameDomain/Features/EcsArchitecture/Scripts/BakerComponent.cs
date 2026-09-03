using Arch.Core;
using Arch.Unity.Conversion;
using UnityEngine;

namespace Project.GameDomain.Features.EcsArchitecture.Scripts
{
    [DisallowMultipleComponent]
    public sealed class BakerComponent : MonoBehaviour
    {
        public Entity Bake(World world)
        {
            return EntityConversion.Convert(
                gameObject,
                world,
                new EntityConversionOptions
                {
                    ConversionMode = ConversionMode.ConvertAndDestroy,
                });
        }
    }
}
