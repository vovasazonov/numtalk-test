using System.Collections.Generic;
using UnityEngine;

namespace Project.GameDomain.Features.Creature.Scripts
{
    [CreateAssetMenu(
        fileName = "CreatureStateConfigDatabase",
        menuName = "IdleStory/Features/Creature/Creature State Config Database")]
    public class CreatureStateConfigDatabase : ScriptableObject
    {
        public List<CreatureStateConfig> Configs = new();

        public bool TryGet(CreatureType type, out CreatureStateConfig config)
        {
            foreach (CreatureStateConfig entry in Configs)
            {
                if (entry != null && entry.Type == type)
                {
                    config = entry;
                    return true;
                }
            }

            config = null;
            return false;
        }
    }
}
