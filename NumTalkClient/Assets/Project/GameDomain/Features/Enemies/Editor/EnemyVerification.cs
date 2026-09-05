using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Project.GameDomain.Features.Configs.Scripts;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using Project.GameDomain.Features.Enemies.Scripts;
using Project.GameDomain.Features.Physics.Scripts;
using Project.GameDomain.Features.Platforms.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using Project.GameDomain.Features.PlayerInput.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Project.GameDomain.Features.Enemies.Editor
{
    /// <summary>
    /// A12 regression checks. Drives the real patrol, shooter and projectile systems against a real Unity
    /// simulation, stepped manually, and always removes its temporary objects and restores the simulation mode.
    /// </summary>
    public static class EnemyVerification
    {
        private const float Dt = 1f / 60f;
        private const int PlayerLayer = 8;
        private const int GroundLayer = 9;
        private const int EnemyLayer = 12;
        private static readonly Vector3 Origin = new(-30000f, 0f, -30000f);

        [MenuItem("NumTalk/Verify Enemies")]
        public static void RunMenu() => Debug.Log(Run());

        public static string Run()
        {
            var tuning = ScriptableObject.CreateInstance<PlatformerTuningConfig>();
            SimulationMode previousMode = UnityEngine.Physics.simulationMode;
            UnityEngine.Physics.simulationMode = SimulationMode.Script;
            try
            {
                VerifyPatrol(tuning);
                VerifyMask(tuning);
                VerifyShooterRangeAndWindUp(tuning);
                VerifyNoTunneling(tuning);
                VerifyKnockback(tuning);
                VerifyPooling(tuning);

                return "A12 passed: patrol bounded by its authored route and reversing after its wait, a shooter that " +
                    "only fires down its fire line after a wind-up, a 400 m/s projectile stopped by a 0.1 m wall with " +
                    "no tunneling, knockback as an external channel that decays while intent still steers, and " +
                    "projectile entities reused from the pool.";
            }
            finally
            {
                UnityEngine.Physics.simulationMode = previousMode;
                UnityEngine.Object.DestroyImmediate(tuning);
            }
        }

        /// <summary>The patrol never leaves its route, waits at the end, and comes back.</summary>
        private static void VerifyPatrol(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning);
            Entity enemy = world.AddPatrol(new float3(0f, 0f, 4f), speed: 2f, waitTime: 0.5f);

            float furthest = 0f;
            for (int tick = 0; tick < 240; tick++)
            {
                world.Run(float2.zero, 1);
                furthest = math.max(furthest, world.Position(enemy).z - Origin.z);
                Check(furthest <= 4f + 0.001f, $"The patrol overshot its route end; it reached {furthest:F3} m of 4 m");
            }

            Check(furthest > 3.99f, $"The patrol did not reach its route end; it only reached {furthest:F3} m");
            Check(world.Position(enemy).z - Origin.z < 3.9f, "The patrol did not turn around and come back");
        }

        /// <summary>The mask is what keeps a shooter from hitting itself, its own shots, or another enemy.</summary>
        private static void VerifyMask(PlatformerTuningConfig tuning)
        {
            int mask = tuning.ProjectileHitMask.value;
            Check((mask & (1 << EnemyLayer)) == 0, "The projectile mask must exclude Enemy");
            Check((mask & (1 << LayerMask.NameToLayer("EnemyProjectile"))) == 0,
                "The projectile mask must exclude EnemyProjectile");
            Check((mask & (1 << PlayerLayer)) != 0, "The projectile mask must include Player");
        }

        private static void VerifyShooterRangeAndWindUp(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning);
            // Fires along +Z; the player sits at the origin, so a short range leaves them out of reach.
            Entity shooter = world.AddShooter(new float3(0f, 1f, -6f), range: 3f, interval: 1f, windUp: 0.5f, speed: 12f);
            world.Run(float2.zero, 120);
            Check(world.LiveProjectiles == 0, "A shooter fired at a player outside its range");

            world.SetRange(shooter, 20f);
            world.Run(float2.zero, 20);
            Check(world.LiveProjectiles == 0, $"A shooter fired during its 0.5 s wind-up ({world.LiveProjectiles} shots)");

            world.Run(float2.zero, 12);
            Check(world.LiveProjectiles == 1, $"A shooter did not fire after its wind-up ({world.LiveProjectiles} shots)");
        }

        /// <summary>A projectile fast enough to clear a thin wall in one step must still be stopped by it.</summary>
        private static void VerifyNoTunneling(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning);
            world.AddWall(Origin + new Vector3(0f, 1f, 3f), thickness: 0.1f);

            Entity projectile = world.Fire(Origin + new Vector3(0f, 1f, 0f), new float3(0f, 0f, 400f));
            float step = 400f * Dt;
            Check(step > 3f, $"The test is not a tunneling test: one step is only {step:F2} m");

            world.Run(float2.zero, 1);
            float3 stopped = world.Position(projectile);
            Check(world.LiveProjectiles == 0, "The projectile was not consumed by the wall");
            Check(stopped.z - Origin.z < 3f, $"The projectile passed through the wall; it stopped at {stopped.z - Origin.z:F2} m");
        }

        /// <summary>Knockback is an external channel: it decays on its own and never takes control away.</summary>
        private static void VerifyKnockback(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning);
            world.Fire(Origin + new Vector3(0f, 1f, -3f), new float3(0f, 0f, 60f));
            float3 knockback = float3.zero;
            for (int tick = 0; tick < 10 && math.lengthsq(knockback) == 0f; tick++)
            {
                world.Run(float2.zero, 1);
                knockback = world.External;
            }

            // The motor already decayed the impulse once on the tick it landed, so this is a band, not an equality.
            Check(knockback.z > tuning.KnockbackSpeed * 0.85f && knockback.z <= tuning.KnockbackSpeed,
                $"A hit must push the player at about {tuning.KnockbackSpeed} m/s, not {knockback.z:F2}");

            // Intent still accelerates the intrinsic channel while the impulse is decaying.
            world.Run(new float2(0f, -1f), 10);
            Check(math.abs(world.External.z) < math.abs(knockback.z),
                $"Knockback did not decay; it is still {world.External.z:F2} m/s");
            Check(world.Motor.Velocity.z < -1f,
                $"The player could not steer against knockback; intrinsic velocity is {world.Motor.Velocity.z:F2} m/s");

            world.Run(float2.zero, 120);
            Check(math.lengthsq(world.External) < 0.01f, "Knockback did not decay to nothing");
        }

        /// <summary>Repeated shots reuse the same entities instead of growing the world.</summary>
        private static void VerifyPooling(PlatformerTuningConfig tuning)
        {
            using var world = new Fixture(tuning);
            world.AddWall(Origin + new Vector3(0f, 1f, 8f), thickness: 0.5f);

            var seen = new HashSet<Entity>();
            for (int shot = 0; shot < 6; shot++)
            {
                seen.Add(world.Fire(Origin + new Vector3(0f, 1f, 6f), new float3(0f, 0f, 400f)));
                world.Run(float2.zero, 1);
            }

            Check(world.LiveProjectiles == 0, "Projectiles that hit the wall were not returned to the pool");
            Check(seen.Count == 1, $"Six shots created {seen.Count} entities instead of reusing one");
        }

        /// <summary>One player on a floor, plus whichever enemies a case needs, and the real systems wired together.</summary>
        private sealed class Fixture : IDisposable
        {
            private readonly World _world;
            private readonly CharacterMotionService _motion = new();
            private readonly ProjectilePool _pool;
            private readonly PlatformerTuningConfig _tuning;
            private readonly UnitySystemBase[] _systems;
            private readonly Entity _player;
            private readonly CharacterBodyComponentListener _listener;
            private readonly List<GameObject> _scratch = new();

            public int LiveProjectiles => _pool.Live.Count;
            public float3 External => _world.Get<ExternalVelocityComponent>(_player).Velocity;
            public PlayerMotorComponent Motor => _world.Get<PlayerMotorComponent>(_player);

            public Fixture(PlatformerTuningConfig tuning)
            {
                _tuning = tuning;
                _world = World.Create();
                _pool = new ProjectilePool(_world);

                var floor = New("EnemyFloor", GroundLayer, Origin + new Vector3(0f, -0.5f, 0f));
                floor.AddComponent<BoxCollider>().size = new Vector3(20f, 1f, 40f);

                var root = New("EnemyPlayer", PlayerLayer, Origin);
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
                root.AddComponent<EntityView>().Initialize(_world, _player);
                _listener.Sync(_world, _player);

                _systems = new UnitySystemBase[]
                {
                    new EnemyPatrolSystem(_world),
                    new ShooterSystem(_world, _pool, tuning),
                    new ProjectileSystem(_world, _pool, tuning),
                    new PlayerMotorSystem(_world, _motion, tuning),
                };
                UnityEngine.Physics.SyncTransforms();
            }

            public float3 Position(Entity entity) => _world.Get<EntityTransformComponent>(entity).Position;

            public Entity AddPatrol(float3 endOffset, float speed, float waitTime)
            {
                return _world.Create(new EnemyComponent(), new StompTargetComponent(),
                    new PatrolComponent
                    {
                        StartPosition = Origin,
                        EndPosition = (float3)Origin + endOffset,
                        Speed = speed,
                        WaitTime = waitTime,
                        IsForward = true,
                    },
                    new EntityTransformComponent { Position = Origin, Rotation = quaternion.identity, Layer = EnemyLayer });
            }

            public Entity AddShooter(float3 offset, float range, float interval, float windUp, float speed)
            {
                float3 position = (float3)Origin + offset;
                return _world.Create(new EnemyComponent(), new StompTargetComponent(),
                    new ShooterComponent
                    {
                        FireDirection = new float3(0f, 0f, 1f),
                        Range = range,
                        FireInterval = interval,
                        WindUpTime = windUp,
                        ProjectileSpeed = speed,
                    },
                    new EntityTransformComponent { Position = position, Rotation = quaternion.identity, Layer = EnemyLayer });
            }

            public void SetRange(Entity shooter, float range) => _world.Get<ShooterComponent>(shooter).Range = range;

            public Entity Fire(float3 position, float3 velocity)
                => _pool.Rent(position, velocity, _tuning.ProjectileRadius, _tuning.ProjectileLifeTime);

            public void AddWall(Vector3 position, float thickness)
            {
                New("EnemyWall", GroundLayer, position).AddComponent<BoxCollider>().size =
                    new Vector3(10f, 4f, thickness);
                UnityEngine.Physics.SyncTransforms();
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
            if (!condition) throw new InvalidOperationException("A12 verification failed: " + label);
        }
    }
}
