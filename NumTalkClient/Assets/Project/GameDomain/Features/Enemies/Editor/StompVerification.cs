using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Enemies.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.PlayerInput.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Project.GameDomain.Features.Enemies.Editor
{
    /// <summary>
    /// A13 regression checks. Every case runs the real <see cref="StompSystem"/> behind the real motor against a
    /// manually stepped Unity simulation, including terminal fall speed at a forced 30 FPS.
    /// </summary>
    public static class StompVerification
    {
        private const int PlayerLayer = 8;
        private const int GroundLayer = 9;
        private const int EnemyLayer = 12;
        private const float EnemyHalfHeight = 0.5f;
        private const float EnemyTop = 1f;
        private static readonly Vector3 Origin = new(-60000f, 0f, -60000f);

        [MenuItem("NumTalk/Verify Stomp")]
        public static void RunMenu() => Debug.Log(Run());

        public static string Run()
        {
            var tuning = ScriptableObject.CreateInstance<PlatformerTuningConfig>();
            SimulationMode previousMode = UnityEngine.Physics.simulationMode;
            UnityEngine.Physics.simulationMode = SimulationMode.Script;
            try
            {
                VerifyTopHit(tuning, 1f / 60f, dropHeight: 3f, jumpHeld: false);
                VerifyHeldBounceIsHigher(tuning);
                VerifySideHit(tuning);
                VerifyUndersideHit(tuning);
                float terminal = VerifyTopHit(tuning, 1f / 60f, dropHeight: 60f, jumpHeld: false);
                float slow = VerifyTopHit(tuning, 1f / 30f, dropHeight: 60f, jumpHeld: false);
                VerifyDefeatedEnemyIsInert(tuning);

                return "A13 passed: swept top hits defeat the enemy and bounce the player, side and underside hits " +
                    $"hurt instead, a defeated enemy stops resolving, and a terminal-speed fall lands on the enemy " +
                    $"top at both 60 FPS ({terminal:F3} m) and a forced 30 FPS ({slow:F3} m) with no pass-through.";
            }
            finally
            {
                UnityEngine.Physics.simulationMode = previousMode;
                UnityEngine.Object.DestroyImmediate(tuning);
            }
        }

        /// <summary>Falls onto the enemy and returns the player's resting height above the enemy top.</summary>
        private static float VerifyTopHit(PlatformerTuningConfig tuning, float dt, float dropHeight, bool jumpHeld)
        {
            using var world = new Fixture(tuning, dt);
            Entity enemy = world.AddEnemy(new float3(0f, EnemyHalfHeight, 4f));
            world.PlacePlayer(new float3(0f, dropHeight, 4f));
            world.SetJumpHeld(jumpHeld);

            float lowest = float.PositiveInfinity;
            for (int tick = 0; tick < 400 && !world.IsDefeated(enemy); tick++)
            {
                world.Run(1);
                lowest = math.min(lowest, world.PlayerPosition.y);
            }

            Check(world.IsDefeated(enemy), $"A fall from {dropHeight} m at {1f / dt:F0} FPS did not defeat the enemy");
            Check(lowest >= Origin.y + EnemyTop - 0.1f,
                $"The player passed through the enemy; it reached {lowest - Origin.y:F3} m against a top of {EnemyTop} m");
            Check(world.Motor.Velocity.y > tuning.StompBounceSpeed - 0.5f,
                $"The stomp did not bounce the player; vertical velocity is {world.Motor.Velocity.y:F2} m/s");

            return world.PlayerPosition.y - Origin.y;
        }

        private static void VerifyHeldBounceIsHigher(PlatformerTuningConfig tuning)
        {
            float released = BounceSpeed(tuning, jumpHeld: false);
            float held = BounceSpeed(tuning, jumpHeld: true);
            Check(math.abs(released - tuning.StompBounceSpeed) < 0.01f,
                $"A stomp must bounce at {tuning.StompBounceSpeed} m/s, not {released:F2}");
            Check(math.abs(held - tuning.HeldJumpStompBounceSpeed) < 0.01f,
                $"A held jump must bounce at {tuning.HeldJumpStompBounceSpeed} m/s, not {held:F2}");
        }

        private static float BounceSpeed(PlatformerTuningConfig tuning, bool jumpHeld)
        {
            using var world = new Fixture(tuning, 1f / 60f);
            Entity enemy = world.AddEnemy(new float3(0f, EnemyHalfHeight, 4f));
            world.PlacePlayer(new float3(0f, 3f, 4f));
            world.SetJumpHeld(jumpHeld);

            for (int tick = 0; tick < 200 && !world.IsDefeated(enemy); tick++) world.Run(1);
            return world.Motor.Velocity.y;
        }

        /// <summary>Walking into an enemy is a hurt contact, never a kill.</summary>
        private static void VerifySideHit(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning, 1f / 60f);
            Entity enemy = world.AddEnemy(new float3(0f, EnemyHalfHeight, 4f));
            world.PlacePlayer(new float3(0f, 0f, 1f));

            for (int tick = 0; tick < 120 && math.lengthsq(world.External) == 0f; tick++) world.Run(1, new float2(0f, 1f));

            Check(!world.IsDefeated(enemy), "Walking into an enemy from the side defeated it");
            Check(world.External.z < -1f,
                $"A side hit must push the player back out; knockback is {world.External.z:F2} m/s");
        }

        /// <summary>Jumping up into an enemy hurts, even though the player and enemy are in contact.</summary>
        private static void VerifyUndersideHit(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning, 1f / 60f);
            Entity enemy = world.AddEnemy(new float3(0f, 3.5f, 4f));
            world.PlacePlayer(new float3(0f, 0f, 4f));
            world.Run(1);
            world.Launch(new float3(0f, 12f, 0f));

            for (int tick = 0; tick < 60 && math.lengthsq(world.External) == 0f; tick++) world.Run(1);

            Check(!world.IsDefeated(enemy), "Hitting an enemy from below defeated it");
            Check(math.lengthsq(world.External) > 0f, "Hitting an enemy from below did not hurt the player");
        }

        /// <summary>Once defeated, the enemy resolves nothing further even while the player is still on top of it.</summary>
        private static void VerifyDefeatedEnemyIsInert(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning, 1f / 60f);
            Entity enemy = world.AddEnemy(new float3(0f, EnemyHalfHeight, 4f));
            world.PlacePlayer(new float3(0f, 3f, 4f));

            for (int tick = 0; tick < 200 && !world.IsDefeated(enemy); tick++) world.Run(1);
            Check(world.IsDefeated(enemy), "The setup fall did not defeat the enemy");
            Check(!world.HasView(enemy), "A defeated enemy kept its view");

            world.ClearExternal();
            world.Run(120);
            Check(math.lengthsq(world.External) == 0f,
                $"A defeated enemy still hurt the player; knockback is {world.External}");
        }

        /// <summary>One player on a floor, one enemy, and the real motor and stomp systems wired together.</summary>
        private sealed class Fixture : IDisposable
        {
            private readonly World _world;
            private readonly CharacterMotionService _motion = new();
            private readonly UnitySystemBase[] _systems;
            private readonly Entity _player;
            private readonly CharacterBodyComponentListener _listener;
            private readonly List<GameObject> _scratch = new();
            private readonly float _dt;

            public float3 PlayerPosition => _world.Get<EntityTransformComponent>(_player).Position;
            public float3 External => _world.Get<ExternalVelocityComponent>(_player).Velocity;
            public PlayerMotorComponent Motor => _world.Get<PlayerMotorComponent>(_player);

            public Fixture(PlatformerTuningConfig tuning, float dt)
            {
                _dt = dt;
                _world = World.Create();

                var floor = New("StompFloor", GroundLayer, Origin + new Vector3(0f, -0.5f, 0f));
                floor.AddComponent<BoxCollider>().size = new Vector3(20f, 1f, 40f);

                var root = New("StompPlayer", PlayerLayer, Origin);
                root.AddComponent<CharacterController>();
                root.AddComponent<CharacterContactRelay>();
                var body = new GameObject("Body");
                body.transform.SetParent(root.transform, false);
                _listener = body.AddComponent<CharacterBodyComponentListener>();
                _listener.Construct(_motion);

                _player = _world.Create(new PlayerTagComponent(), new PlayerMotorComponent(), new JumpStateComponent(),
                    new ExternalVelocityComponent(), new PlatformRiderComponent(), new PlayerInputComponent(),
                    new GroundStateComponent(), new HealthComponent { Lives = 3, MaximumLives = 3 },
                    new EntityTransformComponent { Position = Origin, Rotation = quaternion.identity, Layer = PlayerLayer },
                    new CharacterBodyComponent
                    {
                        Height = 2f, Radius = 0.4f, Center = new float3(0f, 1f, 0f),
                        SlopeLimit = 50f, StepOffset = 0.35f, SkinWidth = 0.04f,
                    });
                root.AddComponent<EntityView>().Initialize(_world, _player);
                _listener.Sync(_world, _player);

                _systems = new UnitySystemBase[]
                {
                    new PlayerMotorSystem(_world, _motion, tuning),
                    new StompSystem(_world, _motion, tuning),
                };
                UnityEngine.Physics.SyncTransforms();
            }

            public Entity AddEnemy(float3 offset)
            {
                Vector3 position = Origin + (Vector3)offset;
                Entity enemy = _world.Create(new EnemyComponent(), new StompTargetComponent(), new ViewComponent(),
                    new EntityTransformComponent { Position = position, Rotation = quaternion.identity, Layer = EnemyLayer });

                var view = New("StompEnemy", EnemyLayer, position);
                view.AddComponent<BoxCollider>().size = new Vector3(1f, EnemyHalfHeight * 2f, 1f);
                view.AddComponent<EntityView>().Initialize(_world, enemy);
                UnityEngine.Physics.SyncTransforms();
                return enemy;
            }

            public bool IsDefeated(Entity enemy) => _world.Get<StompTargetComponent>(enemy).IsDefeated;
            public bool HasView(Entity enemy) => _world.Has<ViewComponent>(enemy);

            public void PlacePlayer(float3 offset)
            {
                _world.Get<EntityTransformComponent>(_player).Position = (float3)(Vector3)Origin + offset;
                UnityEngine.Physics.SyncTransforms();
            }

            public void SetJumpHeld(bool held)
            {
                _world.Get<PlayerInputComponent>(_player).JumpHeld = held;
                _world.Get<JumpStateComponent>(_player).IsHeld = held;
            }

            public void Launch(float3 velocity) => _world.Get<PlayerMotorComponent>(_player).Velocity = velocity;
            public void ClearExternal() => _world.Get<ExternalVelocityComponent>(_player).Velocity = float3.zero;

            public void Run(int ticks) => Run(ticks, float2.zero);

            public void Run(int ticks, float2 move)
            {
                _world.Get<PlayerInputComponent>(_player).Move = move;
                var state = new SystemState { DeltaTime = _dt };
                for (int tick = 0; tick < ticks; tick++)
                {
                    foreach (UnitySystemBase system in _systems) system.Update(in state);
                    UnityEngine.Physics.Simulate(_dt);
                }
            }

            private GameObject New(string name, int layer, Vector3 position)
            {
                var created = new GameObject(name) { layer = layer };
                created.transform.position = position;
                _scratch.Add(created);
                return created;
            }

            public void Dispose()
            {
                _listener.Release();
                foreach (GameObject scratchObject in _scratch) UnityEngine.Object.DestroyImmediate(scratchObject);
                World.Destroy(_world);
            }
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("A13 verification failed: " + label);
        }
    }
}
