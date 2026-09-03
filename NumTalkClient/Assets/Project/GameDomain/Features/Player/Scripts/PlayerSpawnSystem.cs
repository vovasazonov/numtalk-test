using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Creature.Scripts;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Gizmoses.Scripts.Components;
using Project.GameDomain.Features.Movement.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Pickup.Scripts;
using Project.GameDomain.Features.Position.Scripts;
using Project.GameDomain.Features.Universe.Scripts;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.Player.Scripts
{
    public sealed class PlayerSpawnSystem : UnitySystemBase
    {
        public PlayerSpawnSystem(World world) : base(world)
        {
        }

        public override void Initialize()
        {
            var characterUnitSize = UniverseConsts.CalculateUnitsBasePixels(8);
            World.Create(
                new ViewComponent(),
                new PlayerTagComponent(),
                new PickUpCollectorComponent(),
                new PositionComponent
                {
                    Position = new float3(0f, 0f, 0f),
                },
                new MovementComponent
                {
                    Velocity = new float3(0.5f, 0f, 0f),
                },
                new CreatureComponent()
                {
                    Type = CreatureType.Human,
                    State = CreatureState.Idle,
                    Side = CreatureSide.Right
                },
                new GizmosComponent
                {
                    Shape = GizmoShape.Cube,
                    Color = Color.cyan,
                    Offset = Vector3.zero,
                    Radius = characterUnitSize,
                    Size = Vector3.one *  characterUnitSize,
                    IsWireframe = true,
                },
                new RigidbodyComponent
                {
                    IsGravityEnabled = true,
                },
                new ColliderComponent
                {
                    Size = new float3(
                        characterUnitSize,
                        characterUnitSize,
                        characterUnitSize),
                });
        }
    }
}