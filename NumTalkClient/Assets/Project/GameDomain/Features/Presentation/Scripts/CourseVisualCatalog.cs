using System;
using Project.GameDomain.Features.Configs.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    [CreateAssetMenu(menuName = "NumTalk/Course Visual Catalog")]
    public sealed class CourseVisualCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public CourseModel Model;
            public GameObject Prefab;
            public AnimationClip Idle, Walk, Jump, Fall;
        }

        public PlatformerTuningConfig Tuning;
        public Entry[] Entries;

        public Entry Find(CourseModel model)
        {
            foreach (var entry in Entries) if (entry.Model == model) return entry;
            return default;
        }
    }
}
