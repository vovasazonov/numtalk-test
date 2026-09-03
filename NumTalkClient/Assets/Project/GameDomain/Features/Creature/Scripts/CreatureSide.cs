using System;

namespace Project.GameDomain.Features.Creature.Scripts
{
    [Flags]
    public enum CreatureSide
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8,
    }
}
