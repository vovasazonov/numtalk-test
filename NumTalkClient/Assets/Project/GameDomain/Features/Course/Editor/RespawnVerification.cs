using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Checkpoints.Scripts;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.Course.Scripts;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Goal.Scripts;
using Project.GameDomain.Features.Enemies.Scripts;
using Project.GameDomain.Features.Hazards.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Pickup.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.PlayerInput.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Project.GameDomain.Features.Pushables.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Project.GameDomain.Features.Course.Editor
{
    /// <summary>
    /// A14 regression checks. Runs the real trigger, snapshot and respawn systems against a manually stepped Unity
    /// simulation, and exercises the third complete restart rather than only the first.
    /// </summary>
    public static class RespawnVerification
    {
        private const float Dt = 1f / 60f;
        private const int PlayerLayer = 8;
        private const int GroundLayer = 9;
        private const int PushableLayer = 11;
        private const int EnemyLayer = 12;
        private const int PickupLayer = 14;
        private const int KillZoneLayer = 15;
        private static readonly Vector3 Origin = new(90000f, 0f, 90000f);

        [MenuItem("NumTalk/Verify Respawn")]
        public static void RunMenu() => Debug.Log(Run());

        public static string Run()
        {
            var tuning = ScriptableObject.CreateInstance<PlatformerTuningConfig>();
            SimulationMode previousMode = UnityEngine.Physics.simulationMode;
            UnityEngine.Physics.simulationMode = SimulationMode.Script;
            try
            {
                VerifyCheckpointAndKillZone(tuning);
                VerifyCoinsSurviveRespawn(tuning);
                VerifyCourseStateIsRestored(tuning);
                VerifyThirdRestart(tuning);
                VerifyProtection(tuning);
                VerifyGoalAndRestartRequest(tuning);

                return "A14 passed: three lives, a kill plane costing exactly one life per fall, a checkpoint that " +
                    "only moves forward and is resumed from, coins that stay collected across a respawn but come " +
                    "back on a restart, crate/platform/enemy state restored from the snapshot with projectiles " +
                    "returned to the pool, a third complete restart identical to the first, and a goal that " +
                    "completes the run and is cleared by the overlay's restart request.";
            }
            finally
            {
                UnityEngine.Physics.simulationMode = previousMode;
                UnityEngine.Object.DestroyImmediate(tuning);
            }
        }

        private static void VerifyCheckpointAndKillZone(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning);
            Entity checkpoint = world.AddCheckpoint(id: 1, new float3(0f, 0.5f, 4f));
            world.AddKillZone(new float3(0f, -8f, 0f));

            Check(world.Lives == 3, $"The player must start with three lives, not {world.Lives}");

            world.Run(new float2(0f, 1f), 90);
            Check(world.IsActivated(checkpoint), "Walking over a checkpoint did not activate it");
            Check(world.CheckpointId == 1, $"The checkpoint reference was not taken; it is {world.CheckpointId}");

            world.Kill(1);
            Check(world.Lives == 2, $"Falling into the kill plane must cost exactly one life; lives are {world.Lives}");
            // Compared on the ground plane: by now the respawned player has settled onto the floor below the marker.
            Check(math.distance(world.PlayerPosition.xz, world.RespawnPosition.xz) < 0.2f,
                $"The player did not respawn at the checkpoint; they are at {world.PlayerPosition - (float3)Origin}");

            // A lower checkpoint must never pull the run backwards.
            Entity earlier = world.AddCheckpoint(id: 0, world.PlayerPosition - (float3)Origin);
            world.Run(float2.zero, 5);
            Check(world.CheckpointId == 1, "An earlier checkpoint regressed the run");
            Check(!world.IsActivated(earlier), "An earlier checkpoint was activated out of order");
        }

        private static void VerifyCoinsSurviveRespawn(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning);
            world.AddCheckpoint(id: 1, new float3(0f, 0.5f, 3f));
            Entity coin = world.AddCoin(new float3(0f, 0.5f, 5f));
            world.AddKillZone(new float3(0f, -8f, 0f));

            world.Run(new float2(0f, 1f), 120);
            Check(world.IsCollected(coin), "Walking over a coin did not collect it");
            Check(!world.HasView(coin), "A collected coin kept its view");

            world.Kill(1);
            Check(world.IsCollected(coin), "A respawn paid the same coin twice by restoring it");

            world.Kill(2);
            Check(!world.IsCollected(coin), "A full restart did not return the coin to the course");
            Check(world.HasView(coin), "A restored coin did not get its view back");
        }

        /// <summary>The retry has to be fair: the crate, the platform and the enemy all go back where they were.</summary>
        private static void VerifyCourseStateIsRestored(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning);
            world.AddCheckpoint(id: 1, new float3(0f, 0.5f, 2f));
            world.AddKillZone(new float3(0f, -8f, 0f));
            Entity crate = world.AddCrate(new float3(3f, 0.75f, 2f), tuning);
            Entity platform = world.AddMovingPlatform(new float3(-4f, 0.5f, 0f));
            Entity enemy = world.AddEnemy(new float3(6f, 0.5f, 0f));

            // The snapshot is taken on the tick the checkpoint is crossed, so that is the state to compare against.
            for (int tick = 0; tick < 180 && world.CheckpointId == 0; tick++) world.Run(new float2(0f, 1f), 1);
            Check(world.CheckpointId == 1, "The player did not reach the checkpoint");

            float3 crateStart = world.CratePosition(crate);
            float platformStart = world.Platform(platform).Progress;

            world.Nudge(crate, new float3(0f, 0f, 6f));
            world.Defeat(enemy);
            world.Fire(new float3(0f, 2f, 0f));
            world.Run(float2.zero, 60);
            Check(math.distance(world.CratePosition(crate), crateStart) > 0.5f, "The crate did not move before the death");
            Check(world.Platform(platform).Progress != platformStart, "The platform did not move before the death");

            world.Kill(1);

            Check(math.distance(world.CratePosition(crate), crateStart) < 0.05f,
                $"The crate was not restored; it is {math.distance(world.CratePosition(crate), crateStart):F2} m away");
            Check(math.abs(world.Platform(platform).Progress - platformStart) < 0.0001f, "The platform phase was not restored");
            Check(!world.IsDefeated(enemy), "The defeated enemy was not restored by the checkpoint snapshot");
            Check(world.HasView(enemy), "The restored enemy did not get its view back");
            Check(world.LiveProjectiles == 0, "Projectiles in flight were not returned to the pool on respawn");
        }

        private static void VerifyProtection(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning);
            world.AddKillZone(new float3(0f, -8f, 0f));
            world.Kill(1);
            world.DamageDuringProtection();
            Check(world.Lives == 2, "Blink protection allowed another combat death");
            world.Run(float2.zero, 58);
            Check(world.Phase == PlayerLifePhase.Respawning, "Blink protection ended before two seconds");
            world.Run(float2.zero, 3);
            Check(world.Phase == PlayerLifePhase.Alive, "Blink protection never ended");
            world.Kill(1);
            world.PlacePlayer(new float3(0f, -8f, 0f));
            world.Run(float2.zero, 1);
            Check(world.Phase == PlayerLifePhase.Dying, "Falling out during protection stranded the player");
        }

        /// <summary>Three complete restarts, each one landing on exactly the same course state as the first.</summary>
        private static void VerifyThirdRestart(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning);
            world.AddCheckpoint(id: 1, new float3(0f, 0.5f, 3f));
            Entity coin = world.AddCoin(new float3(0f, 0.5f, 5f));
            world.AddKillZone(new float3(0f, -8f, 0f));
            Entity crate = world.AddCrate(new float3(3f, 0.75f, 2f), tuning);
            Entity enemy = world.AddEnemy(new float3(6f, 0.5f, 0f));

            float3 crateStart = world.CratePosition(crate);
            float3 playerStart = world.PlayerPosition;

            for (int restart = 1; restart <= 3; restart++)
            {
                world.WaitUntilReady();
                world.Run(new float2(0f, 1f), 120);
                world.Nudge(crate, new float3(0f, 0f, 6f));
                world.Defeat(enemy);
                world.Run(float2.zero, 60);
                Check(world.CheckpointId == 1, $"Restart {restart}: the checkpoint was not reached before the deaths");

                world.Kill(3);

                Check(world.Lives == 3, $"Restart {restart}: lives are {world.Lives} instead of a full three");
                Check(world.CheckpointId == 0, $"Restart {restart}: the checkpoint reference survived the restart");
                Check(!world.IsCollected(coin), $"Restart {restart}: the coin stayed collected");
                Check(!world.IsDefeated(enemy), $"Restart {restart}: the enemy stayed defeated");
                Check(math.distance(world.CratePosition(crate), crateStart) < 0.05f,
                    $"Restart {restart}: the crate did not return to its authored pose");
                Check(math.distance(world.PlayerPosition.xz, playerStart.xz) < 0.2f,
                    $"Restart {restart}: the player did not return to the run start");
                Check(world.LiveProjectiles == 0, $"Restart {restart}: projectiles survived the restart");
            }
        }

        /// <summary>Reaching the flag ends the run, and the overlay's restart puts the course back to its start.</summary>
        private static void VerifyGoalAndRestartRequest(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning);
            world.AddCheckpoint(id: 1, new float3(0f, 0.5f, 3f));
            Entity coin = world.AddCoin(new float3(0f, 0.5f, 5f));
            Entity goal = world.AddGoal(new float3(0f, 0.5f, 8f));

            for (int tick = 0; tick < 240 && !world.IsComplete; tick++) world.Run(new float2(0f, 1f), 1);

            Check(world.IsReached(goal), "Reaching the flag did not mark the goal");
            Check(world.IsComplete, "Reaching the flag did not complete the run");
            Check(world.IsCollected(coin), "The route to the goal did not pass the coin");

            world.RequestRestart();
            world.Run(float2.zero, 5);

            Check(!world.IsComplete, "The restart request did not clear the finished run");
            Check(world.CheckpointId == 0, "The restart did not clear the checkpoint reference");
            Check(!world.IsCollected(coin), "The restart did not return the coin to the course");
            Check(world.Lives == 3, $"The restart left {world.Lives} lives instead of three");
        }

        /// <summary>The player, a floor, and whatever course pieces a case needs, on the real system schedule.</summary>
        private sealed class Fixture : IDisposable
        {
            private readonly World _world;
            private readonly CharacterMotionService _motion = new();
            private readonly RigidBodyService _bodies = new();
            private readonly ProjectilePool _projectiles;
            private readonly PlatformerTuningConfig _tuning;
            private readonly UnitySystemBase[] _systems;
            private readonly Entity _player;
            private readonly CharacterBodyComponentListener _listener;
            private readonly List<GameObject> _scratch = new();
            private readonly Dictionary<Entity, Rigidbody> _crates = new();

            public PlayerLifePhase Phase => _world.Get<HealthComponent>(_player).Phase;
            public void DamageDuringProtection()
            {
                for (int i = 0; i < 60; i++)
                {
                    _world.Get<HealthComponent>(_player).PendingDamage = 3;
                    Run(float2.zero, 1);
                }
            }
            public int Lives => _world.Get<HealthComponent>(_player).Lives;
            public int CheckpointId => _world.Get<CheckpointReferenceComponent>(_player).CheckpointId;
            public float3 RespawnPosition => _world.Get<CheckpointReferenceComponent>(_player).RespawnPosition;
            public float3 PlayerPosition => _world.Get<EntityTransformComponent>(_player).Position;
            public int LiveProjectiles => _projectiles.Live.Count;

            public Fixture(PlatformerTuningConfig tuning)
            {
                _tuning = tuning;
                _world = World.Create();
                _projectiles = new ProjectilePool(_world);
                var snapshots = new CourseSnapshotService(_world, _bodies, _projectiles);

                var floor = New("RespawnFloor", GroundLayer, Origin + new Vector3(0f, -0.5f, 0f));
                floor.AddComponent<BoxCollider>().size = new Vector3(40f, 1f, 40f);

                var root = New("RespawnPlayer", PlayerLayer, Origin);
                root.AddComponent<CharacterController>();
                root.AddComponent<CharacterContactRelay>();
                var body = new GameObject("Body");
                body.transform.SetParent(root.transform, false);
                _listener = body.AddComponent<CharacterBodyComponentListener>();
                _listener.Construct(_motion);

                _player = _world.Create(new PlayerTagComponent(), new PlayerMotorComponent(), new JumpStateComponent(),
                    new ExternalVelocityComponent(), new PlatformRiderComponent(), new PlayerInputComponent(),
                    new GroundStateComponent(), new HealthComponent { Lives = 3, MaximumLives = 3 }, new RunStateComponent(),
                    new CheckpointReferenceComponent { RespawnPosition = Origin },
                    new InitialStateComponent { Position = Origin, Rotation = quaternion.identity },
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
                    new MovingPlatformSystem(_world),
                    new PlayerMotorSystem(_world, _motion, tuning),
                    new PushableBodySystem(_world, _bodies),
                    new ProjectileSystem(_world, _projectiles, tuning),
                    new StompSystem(_world, _motion, tuning),
                    new CourseTriggerSystem(_world, _motion, snapshots, tuning),
                    new RespawnSystem(_world, snapshots, tuning),
                };
                UnityEngine.Physics.SyncTransforms();
            }

            public Entity AddCheckpoint(int id, float3 offset)
            {
                Vector3 position = Origin + (Vector3)offset;
                Entity checkpoint = _world.Create(new CheckpointComponent
                {
                    Id = id,
                    RespawnPosition = position + Vector3.up * 1.2f,
                }, new EntityTransformComponent { Position = position, Rotation = quaternion.identity, Layer = PickupLayer });

                Trigger("Checkpoint", PickupLayer, position, checkpoint);
                return checkpoint;
            }

            public Entity AddCoin(float3 offset)
            {
                Vector3 position = Origin + (Vector3)offset;
                Entity coin = _world.Create(new PickupComponent { Id = 1, Value = 1 }, new ViewComponent(),
                    new InitialStateComponent { Position = position, Rotation = quaternion.identity },
                    new EntityTransformComponent { Position = position, Rotation = quaternion.identity, Layer = PickupLayer });

                Trigger("Coin", PickupLayer, position, coin);
                return coin;
            }

            public Entity AddKillZone(float3 offset)
            {
                Vector3 position = Origin + (Vector3)offset;
                Entity zone = _world.Create(new KillZoneComponent(),
                    new EntityTransformComponent { Position = position, Rotation = quaternion.identity, Layer = KillZoneLayer });

                Trigger("KillZone", KillZoneLayer, position, zone, new Vector3(60f, 2f, 60f));
                return zone;
            }

            public Entity AddCrate(float3 offset, PlatformerTuningConfig tuning)
            {
                Vector3 position = Origin + (Vector3)offset;
                var crate = New("RespawnCrate", PushableLayer, position);
                crate.AddComponent<BoxCollider>().size = new Vector3(1.5f, 1.5f, 1.5f);
                Rigidbody rigidBody = crate.AddComponent<Rigidbody>();
                rigidBody.mass = tuning.CrateMass;
                rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
                rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                Entity entity = _world.Create(new PushableComponent { PushAcceleration = tuning.CratePushAcceleration },
                    new PhysicsBodyComponent { Mass = tuning.CrateMass, FreezeRotation = true },
                    new PlatformSurfaceComponent { IsStandable = true }, new ViewComponent(),
                    new EntityTransformComponent { Position = position, Rotation = quaternion.identity, Layer = PushableLayer });

                _bodies.Register(entity, rigidBody);
                _crates[entity] = rigidBody;
                crate.AddComponent<EntityView>().Initialize(_world, entity);
                UnityEngine.Physics.SyncTransforms();
                return entity;
            }

            public Entity AddMovingPlatform(float3 offset)
            {
                Vector3 position = Origin + (Vector3)offset;
                return _world.Create(new PlatformSurfaceComponent { IsStandable = true }, new ViewComponent(),
                    new PlatformMotionComponent
                    {
                        StartPosition = position,
                        EndPosition = position + Vector3.forward * 6f,
                        Speed = 2f,
                        IsForward = true,
                    },
                    new EntityTransformComponent { Position = position, Rotation = quaternion.identity, Layer = 10 });
            }

            public Entity AddEnemy(float3 offset)
            {
                Vector3 position = Origin + (Vector3)offset;
                return _world.Create(new EnemyComponent(), new StompTargetComponent(), new ViewComponent(),
                    new EntityTransformComponent { Position = position, Rotation = quaternion.identity, Layer = EnemyLayer });
            }

            public void Fire(float3 offset) => _projectiles.Rent(Origin + (Vector3)offset, new float3(0f, 0f, 4f),
                _tuning.ProjectileRadius, _tuning.ProjectileLifeTime);

            public bool IsComplete => _world.Get<RunStateComponent>(_player).IsComplete;
            public bool IsReached(Entity goal) => _world.Get<GoalComponent>(goal).IsReached;

            public Entity AddGoal(float3 offset)
            {
                Vector3 position = Origin + (Vector3)offset;
                Entity goal = _world.Create(new GoalComponent(),
                    new EntityTransformComponent { Position = position, Rotation = quaternion.identity, Layer = PickupLayer });

                Trigger("Goal", PickupLayer, position, goal);
                return goal;
            }

            public void RequestRestart() => _world.Get<RunStateComponent>(_player).RestartRequested = true;

            public bool IsActivated(Entity checkpoint) => _world.Get<CheckpointComponent>(checkpoint).IsActivated;
            public bool IsCollected(Entity coin) => _world.Get<PickupComponent>(coin).IsCollected;
            public bool IsDefeated(Entity enemy) => _world.Get<StompTargetComponent>(enemy).IsDefeated;
            public bool HasView(Entity entity) => _world.Has<ViewComponent>(entity);
            public PlatformMotionComponent Platform(Entity platform) => _world.Get<PlatformMotionComponent>(platform);
            public float3 CratePosition(Entity crate) => _crates[crate].position;

            public void Nudge(Entity crate, float3 velocity) => _crates[crate].linearVelocity = velocity;
            public void Defeat(Entity enemy) => _world.Get<StompTargetComponent>(enemy).IsDefeated = true;

            public void PlacePlayer(float3 offset)
            {
                _world.Get<EntityTransformComponent>(_player).Position = (float3)(Vector3)Origin + offset;
                UnityEngine.Physics.SyncTransforms();
            }

            /// <summary>
            /// Dies in the kill plane the given number of times, stopping on the tick each death resolves so the
            /// restored state can be read before anything moves again.
            /// </summary>
            public void WaitUntilReady()
            {
                for (int i = 0; i < 130 && _world.Get<HealthComponent>(_player).IsProtected; i++) Run(float2.zero, 1);
            }

            public void Kill(int times)
            {
                for (int death = 0; death < times; death++)
                {
                    while (_world.Get<HealthComponent>(_player).Phase == PlayerLifePhase.Respawning) Run(float2.zero, 1);
                    // Placed inside the kill volume, not above it: this is a death, not a fall.
                    PlacePlayer(new float3(0f, -8f, 0f));
                    int before = Lives;
                    for (int tick = 0; tick < 20 && Lives == before; tick++) Run(float2.zero, 1);
                    Check(_world.Get<HealthComponent>(_player).Phase == PlayerLifePhase.Dying, "Death did not begin its animation phase");
                    float3 deathPosition = PlayerPosition;
                    Run(new float2(1, 1), 60);
                    Check(math.distance(PlayerPosition, deathPosition) < 0.001f, "Dying player moved before respawn");
                    Check(_world.Get<HealthComponent>(_player).Phase == PlayerLifePhase.Dying, "Death animation ended before two seconds");
                    for (int tick = 0; tick < 65 && _world.Get<HealthComponent>(_player).Phase == PlayerLifePhase.Dying; tick++) Run(float2.zero, 1);
                    Check(_world.Get<HealthComponent>(_player).Phase == PlayerLifePhase.Respawning, "Death did not transition to blinking respawn");
                }
            }

            public void Run(float2 move, int ticks)
            {
                _world.Get<PlayerInputComponent>(_player).Move = move;
                var state = new SystemState { DeltaTime = Dt };
                for (int tick = 0; tick < ticks; tick++)
                {
                    foreach (UnitySystemBase system in _systems) system.Update(in state);
                    UnityEngine.Physics.Simulate(Dt);
                }
            }

            private void Trigger(string name, int layer, Vector3 position, Entity entity, Vector3? size = null)
            {
                var created = New(name, layer, position);
                BoxCollider box = created.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = size ?? new Vector3(1f, 2f, 1f);
                created.AddComponent<EntityView>().Initialize(_world, entity);
                UnityEngine.Physics.SyncTransforms();
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
            if (!condition) throw new InvalidOperationException("A14 verification failed: " + label);
        }
    }
}
