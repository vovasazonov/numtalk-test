using Arch.Unity.Conversion;
using Project.GameDomain.Features.Configs.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    public sealed class FlashFreezeBaker : MonoBehaviour, IComponentConverter
    {
        public PlatformerTuningConfig Tuning;
        public float TriggerZ = 109f;

        public void Convert(IEntityConverter converter) => converter.AddComponent(new FlashFreezeComponent
        {
            TriggerZ = TriggerZ,
            WarningSeconds = Tuning.FreezeWarningSeconds,
            FrozenSeconds = Tuning.FreezeDurationSeconds,
            DecelerationScale = Tuning.IceDecelerationScale,
        });
    }
}
