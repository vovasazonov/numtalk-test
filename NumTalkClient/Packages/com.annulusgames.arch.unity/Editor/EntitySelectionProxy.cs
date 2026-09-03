using System;
using Arch.Core;
using UnityEngine;

namespace Arch.Unity.Editor
{
    public sealed class EntitySelectionProxy : ScriptableObject
    {
        [NonSerialized] public World world;
        [NonSerialized] public Entity entity;
    }
}