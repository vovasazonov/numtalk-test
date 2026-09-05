using Arch.Unity.Conversion;
using Project.GameDomain.Features.Course.Scripts;
using Project.GameDomain.Features.PlayerInput.Scripts;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.Player.Scripts
{
    /// <summary>
    /// Player character authoring. Pose, shape, collider and character body come from their own bakers on the same
    /// object; this one writes only the gameplay state the fixed-step systems own.
    /// </summary>
    public sealed class PlayerBaker : MonoBehaviour, IComponentConverter
    {
        [Header("Lives")]
        [SerializeField, Min(1)] private int _lives = 3;

        private void Reset()
        {
            gameObject.layer = LayerMask.NameToLayer("Player");
        }

        public void Convert(IEntityConverter converter)
        {
            converter.AddComponent(new PlayerTagComponent());
            converter.AddComponent(new InitialStateComponent
            {
                Position = transform.position,
                Rotation = transform.rotation,
            });
            converter.AddComponent(new PlayerInputComponent());
            converter.AddComponent(new PlayerMotorComponent());
            converter.AddComponent(new JumpStateComponent());
            converter.AddComponent(new GroundStateComponent { GroundNormal = math.up() });
            converter.AddComponent(new ExternalVelocityComponent());
            converter.AddComponent(new PlatformRiderComponent());
            converter.AddComponent(new HealthComponent { Lives = _lives, MaximumLives = _lives });
            converter.AddComponent(new CheckpointReferenceComponent
            {
                RespawnPosition = transform.position,
            });
        }
    }
}
