using UnityEngine;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    public enum CourseModel { Primitive, Grass, Ice, Moving, Crumble, Crate, Player, Patrol, Shooter, Coin, Checkpoint, Goal }

    /// <summary>Optional art override. ShapeBaker copies this as values; collision bakers keep their original geometry.</summary>
    public sealed class CourseVisualBaker : MonoBehaviour
    {
        public CourseModel Model;
        public Vector3 Size = Vector3.one;
        public Vector3 Offset;
    }
}
