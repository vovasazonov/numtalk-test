using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.PlayerInput.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Project.GameDomain.Features.Pushables.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Project.GameDomain.Features.Pushables.Editor
{
    /// <summary>
    /// A11 regression checks. Drives the real systems against a real Unity simulation, stepped manually, and always
    /// removes its temporary objects and restores the project's simulation mode.
    /// </summary>
    public static class CrateVerification
    {
        private const float Dt = 1f / 60f;
        private const int PlayerLayer = 8;
        private const int GroundLayer = 9;
        private const int PushableLayer = 11;
        private const float CrateHalfExtent = 0.75f;
        private static readonly Vector3 Origin = new(30000f, 0f, 30000f);

        [MenuItem("NumTalk/Verify Crate")]
        public static void RunMenu() => Debug.Log(Run());

        public static string Run()
        {
            var tuning = ScriptableObject.CreateInstance<PlatformerTuningConfig>();
            SimulationMode previousMode = UnityEngine.Physics.simulationMode;
            UnityEngine.Physics.simulationMode = SimulationMode.Script;
            try
            {
                float pushed = PushDistance(tuning, 0.55f);
                Check(pushed > 0.5f, $"A shove moves the crate away from the player; it moved {pushed:F2} m");

                float onIce = PushDistance(tuning, 0.04f);
                Check(onIce > pushed * 1.5f, $"The same shove carries the crate further on ice ({onIce:F2} m vs {pushed:F2} m)");

                VerifyWall(tuning, pushed);

                VerifyNoShoveThrough(tuning);
                VerifyEdgeAndRide(tuning);

                return "A11 passed: mass-independent shove, ice carrying the crate further than default friction, " +
                    "a wall stopping it at the face without penetration, the player never shoved through it, " +
                    "the crate falling off an edge, and standing on it feeding the same rider channel as a platform.";
            }
            finally
            {
                UnityEngine.Physics.simulationMode = previousMode;
                UnityEngine.Object.DestroyImmediate(tuning);
            }
        }

        /// <summary>A crate shoved into a wall stops at its face instead of being forced through it.</summary>
        private static void VerifyWall(PlatformerTuningConfig tuning, float unobstructed)
        {
            const float wallCentre = 4.3f;
            const float wallFace = wallCentre - 0.25f;
            using var world = new Fixture(tuning, 0.55f);
            world.AddWall(Origin + new Vector3(0f, 1f, wallCentre));

            float3 start = world.CratePosition;
            world.Run(new float2(0f, 1f), 120);
            float travelled = world.CratePosition.z - start.z;

            Check(travelled < unobstructed - 0.5f,
                $"A wall stops the crate short of its free travel; it moved {travelled:F2} m of {unobstructed:F2} m");
            float crateFace = world.CratePosition.z - Origin.z + CrateHalfExtent;
            Check(crateFace <= wallFace + 0.05f,
                $"The crate does not penetrate the wall; its face reached {crateFace:F3} against {wallFace:F3}");
        }

        /// <summary>Walks the player into a crate for one second and reports how far the crate travelled.</summary>
        private static float PushDistance(PlatformerTuningConfig tuning, float friction)
        {
            using var world = new Fixture(tuning, friction);
            float3 start = world.CratePosition;
            world.Run(new float2(0f, 1f), 60);
            return world.CratePosition.z - start.z;
        }

        /// <summary>The crate must resist the player, never displace them through it.</summary>
        private static void VerifyNoShoveThrough(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning, 0.55f);
            world.AddWall(Origin + new Vector3(0f, 1f, 4.3f));
            world.Run(new float2(0f, 1f), 120);
            Check(world.PlayerPosition.z < world.CratePosition.z - 0.4f,
                $"Player at {world.PlayerPosition.z:F2} passed through the crate at {world.CratePosition.z:F2}");
        }

        private static void VerifyEdgeAndRide(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning, 0.55f);

            // Standing on the crate feeds the shared rider channel, exactly like a moving platform does.
            world.PlaceOnCrate();
            world.Run(float2.zero, 20);
            Check(world.Rider.Platform == world.Crate, "Standing on the crate makes it the ridden surface");
            world.Nudge(new float3(0f, 0f, 3f));
            world.Run(float2.zero, 2);
            Check(math.abs(world.Rider.SurfaceVelocity.z) > 0.5f, "A moving crate carries its rider");

            // The crate falls when it leaves the ground, and stops being a surface once it is gone.
            world.Nudge(new float3(0f, 0f, 40f));
            world.Run(float2.zero, 90);
            Check(world.CratePosition.y < -1f, $"A crate pushed off an edge falls; it is at y {world.CratePosition.y:F2}");
        }

        /// <summary>One player, one crate, one floor, and the real systems wired together.</summary>
        private sealed class Fixture : IDisposable
        {
            private readonly World _world;
            private readonly CharacterMotionService _motion = new();
            private readonly RigidBodyService _bodies = new();
            private readonly UnitySystemBase[] _systems;
            private readonly Entity _player;
            private readonly Rigidbody _crateBody;
            private readonly CharacterBodyComponentListener _listener;

            public Entity Crate { get; }

            public float3 CratePosition => _crateBody.position;
            public float3 PlayerPosition => _world.Get<EntityTransformComponent>(_player).Position;
            public PlatformRiderComponent Rider => _world.Get<PlatformRiderComponent>(_player);

            public Fixture(PlatformerTuningConfig tuning, float friction)
            {
                _world = World.Create();

                var floor = New("CrateFloor", GroundLayer, Origin + new Vector3(0f, -0.5f, 4f));
                var floorCollider = floor.AddComponent<BoxCollider>();
                floorCollider.size = new Vector3(10f, 1f, 20f);
                floorCollider.sharedMaterial = Material(friction);

                var crate = New("Crate", PushableLayer, Origin + new Vector3(0f, 0.75f, 3f));
                var crateCollider = crate.AddComponent<BoxCollider>();
                crateCollider.size = new Vector3(1.5f, 1.5f, 1.5f);
                crateCollider.sharedMaterial = Material(friction);
                _crateBody = crate.AddComponent<Rigidbody>();
                _crateBody.mass = tuning.CrateMass;
                _crateBody.constraints = RigidbodyConstraints.FreezeRotation;
                _crateBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                Crate = _world.Create(
                    new PushableComponent { PushAcceleration = tuning.CratePushAcceleration },
                    new PhysicsBodyComponent { Mass = tuning.CrateMass, FreezeRotation = true },
                    new PlatformSurfaceComponent { IsStandable = true },
                    new EntityTransformComponent
                    {
                        Position = crate.transform.position, Rotation = quaternion.identity, Layer = PushableLayer,
                    });
                _bodies.Register(Crate, _crateBody);
                crate.AddComponent<EntityView>().Initialize(_world, Crate);

                var root = New("CratePlayer", PlayerLayer, Origin);
                root.AddComponent<CharacterController>();
                root.AddComponent<CharacterContactRelay>();
                var body = new GameObject("Body");
                body.transform.SetParent(root.transform, false);
                _listener = body.AddComponent<CharacterBodyComponentListener>();
                _listener.Construct(_motion);

                _player = _world.Create(new PlayerTagComponent(), new PlayerMotorComponent(), new JumpStateComponent(),
                    new ExternalVelocityComponent(), new PlatformRiderComponent(), new PlayerInputComponent(),
                    new GroundStateComponent(),
                    new EntityTransformComponent { Position = Origin, Rotation = quaternion.identity, Layer = PlayerLayer },
                    new CharacterBodyComponent
                    {
                        Height = 2f, Radius = 0.4f, Center = new float3(0f, 1f, 0f),
                        SlopeLimit = 50f, StepOffset = 0.35f, SkinWidth = 0.04f,
                    });
                _listener.Sync(_world, _player);

                _systems = new UnitySystemBase[]
                {
                    new PlatformRiderSystem(_world),
                    new PlayerMotorSystem(_world, _motion, tuning),
                    new CratePushSystem(_world, _motion, _bodies, tuning),
                    new PushableBodySystem(_world, _bodies),
                };
                UnityEngine.Physics.SyncTransforms();
            }

            public void AddWall(Vector3 position)
            {
                New("CrateWall", GroundLayer, position).AddComponent<BoxCollider>().size = new Vector3(10f, 4f, 0.5f);
                UnityEngine.Physics.SyncTransforms();
            }

            public void PlaceOnCrate()
            {
                _world.Get<EntityTransformComponent>(_player).Position = CratePosition + new float3(0f, 0.8f, 0f);
                UnityEngine.Physics.SyncTransforms();
            }

            public void Nudge(float3 velocity) => _crateBody.linearVelocity = velocity;

            public void Run(float2 move, int ticks)
            {
                ref var input = ref _world.Get<PlayerInputComponent>(_player);
                input.Move = move;
                var state = new SystemState { DeltaTime = Dt };
                for (int tick = 0; tick < ticks; tick++)
                {
                    foreach (UnitySystemBase system in _systems) system.Update(in state);
                    UnityEngine.Physics.Simulate(Dt);
                }
            }

            private readonly List<GameObject> _scratch = new();

            private GameObject New(string name, int layer, Vector3 position)
            {
                var created = new GameObject(name) { layer = layer };
                created.transform.position = position;
                _scratch.Add(created);
                return created;
            }

            private static PhysicsMaterial Material(float friction) => new("CrateVerification")
            {
                dynamicFriction = friction,
                staticFriction = friction + 0.1f,
                bounciness = 0f,
                hideFlags = HideFlags.HideAndDontSave,
            };

            public void Dispose()
            {
                _listener.Release();
                foreach (GameObject scratchObject in _scratch) UnityEngine.Object.DestroyImmediate(scratchObject);
                World.Destroy(_world);
            }
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("A11 verification failed: " + label);
        }
    }
}
