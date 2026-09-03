using Arch.Unity.Conversion;
using UnityEngine;

namespace Project.GameDomain.Features.Creature.Scripts
{
    public sealed class CreatureBaker : MonoBehaviour, IComponentConverter
    {
        [SerializeField] private CreatureType _type = CreatureType.Human;
        [SerializeField] private CreatureState _state = CreatureState.Idle;
        [SerializeField] private CreatureSide _side = CreatureSide.Right;

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new CreatureComponent
            {
                Type = _type,
                State = _state,
                Side = _side,
            });
        }
    }
}
