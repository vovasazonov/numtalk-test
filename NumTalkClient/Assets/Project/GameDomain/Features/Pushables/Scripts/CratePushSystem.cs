using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Unity.Mathematics;

namespace Project.GameDomain.Features.Pushables.Scripts
{
    /// <summary>
    /// Turns the player's swept controller contacts into a horizontal shove on whatever pushable it walked into.
    /// The player is never displaced in return: the swept move is already blocked by the crate collider, so
    /// resistance is felt through the motor rather than by physics shoving the character.
    /// </summary>
    public sealed class CratePushSystem : UnitySystemBase
    {
        /// <summary>A contact steeper than this is a floor or a ceiling, not something to push against.</summary>
        private const float MaximumPushNormalRise = 0.4f;

        private readonly CharacterMotionService _motion;
        private readonly RigidBodyService _bodies;
        private readonly PlatformerTuningConfig _tuning;

        private readonly QueryDescription _players = new QueryDescription()
            .WithAll<PlayerTagComponent, PlayerMotorComponent, CharacterBodyComponent>();

        private readonly ForEach _push;

        public CratePushSystem(World world, CharacterMotionService motion, RigidBodyService bodies,
            PlatformerTuningConfig tuning) : base(world)
        {
            _motion = motion;
            _bodies = bodies;
            _tuning = tuning;
            _push = Push;
        }

        public override void Update(in SystemState state) => World.Query(in _players, _push);

        private void Push(Entity entity)
        {
            if (!_motion.IsReady(entity)) return;

            float3 velocity = World.Get<PlayerMotorComponent>(entity).Velocity;
            IReadOnlyList<CharacterContact> contacts = _motion.DrainContacts(entity);

            for (int index = 0; index < contacts.Count; index++)
            {
                CharacterContact contact = contacts[index];
                if (!World.IsAlive(contact.Other) || !_bodies.IsReady(contact.Other)) continue;
                if (!World.TryGet(contact.Other, out PushableComponent pushable)) continue;
                if (math.abs(contact.Normal.y) > MaximumPushNormalRise) continue;

                // Push along the contact face, and only as hard as the player is actually leaning into it.
                float3 direction = math.normalizesafe(new float3(-contact.Normal.x, 0f, -contact.Normal.z));
                float lean = math.dot(new float3(velocity.x, 0f, velocity.z), direction);
                if (lean <= 0f) continue;

                // Stop pushing once the crate already matches the player, so it is shoved rather than launched.
                _bodies.Read(contact.Other, out _, out _, out float3 crateVelocity);
                if (math.dot(crateVelocity, direction) >= math.min(lean, _tuning.MaximumRunSpeed)) continue;

                _bodies.Accelerate(contact.Other, direction * pushable.PushAcceleration);
            }
        }
    }
}
