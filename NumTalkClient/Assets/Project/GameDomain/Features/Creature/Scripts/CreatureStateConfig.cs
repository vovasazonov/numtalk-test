using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.GameDomain.Features.Creature.Scripts
{
    [CreateAssetMenu(
        fileName = "CreatureStateConfig",
        menuName = "IdleStory/Features/Creature/Creature State Config")]
    public class CreatureStateConfig : ScriptableObject
    {
        public CreatureType Type;
        public List<StateSprites> States = new();

        public bool TryGet(CreatureState state, out StateSprites sprites)
        {
            foreach (StateSprites entry in States)
            {
                if (entry.State == state)
                {
                    sprites = entry;
                    return true;
                }
            }

            sprites = null;
            return false;
        }

        [Serializable]
        public class StateSprites
        {
            public CreatureState State;
            public bool IsOneShot;

            public Sprite[] DownRight = Array.Empty<Sprite>();
            public Sprite[] DownLeft = Array.Empty<Sprite>();
            public Sprite[] UpRight = Array.Empty<Sprite>();
            public Sprite[] UpLeft = Array.Empty<Sprite>();

            public Sprite[] ShadowDownRight = Array.Empty<Sprite>();
            public Sprite[] ShadowDownLeft = Array.Empty<Sprite>();
            public Sprite[] ShadowUpRight = Array.Empty<Sprite>();
            public Sprite[] ShadowUpLeft = Array.Empty<Sprite>();
        }
    }
}
