# Engineering Decisions

## 1. Gameplay architecture - ECS

I considered ECS, MVC, and MVP for the whole project. I chose **ECS with data-oriented design** for gameplay because this platformer has many independent mechanics that must compose: movement, jump forgiveness, moving/ice/crumble platforms, crate physics, enemies, knockback, pickups, and checkpoints. A feature is data plus a focused system, so it is easier to add or remove without changing unrelated behavior.

**Trade-off:** ECS needs a small bridge to Unity objects and is less convenient for presentation-heavy flows. I accept that cost because the game is simulation-heavy.

## 2. Scene authoring - pure ECS conversion, authoring-only scene

`ArenaScene` is an authoring artifact. Artists lay the course out there with real meshes, colliders and semantic bakers, and every baker serialises what it needs into plain ECS data. At runtime the bake is `ConvertAndDestroy`: the authored GameObjects are gone and the Arch world is the only game state.

The runtime representation is rebuilt from that data by the existing view pipeline. `ViewSystem` spawns one `EntityView` root per entity, and each ECS component that needs a view gets its own `ComponentListener` child. Unity components that must sit on the root - `Rigidbody`, `CharacterController` - are declared by the listeners that need them and reference-counted by `EntityView`: added on first request, destroyed when the last requiring ECS component is removed. So one crate carrying both `PushableComponent` and `PhysicsBodyComponent` gets exactly one Rigidbody, and it survives losing either component alone.

**Trade-off:** the authored look must survive as data, so shape, size, colour and collider volume are baked into components rather than kept as object references. The course is built from Unity primitives, which the brief requires anyway, so this is cheap. The cost is one extra GameObject per viewed component and an addressable load per listener type, paid once at level load.

## 2b. Composable platform behavior - the timed fourth-behavior result

`PlatformSurfaceComponent` is the only thing the player motor knows about a surface: a velocity to inherit and whether it is standable. Motion, ice, and crumble are independent components on the same entity, and `PlatformRiderSystem` is the single place that resolves whichever of them the player happens to be standing on into the rider's velocity channel and surface slip.

To test the claim, I built the shared contract with Moving and Ice first, verified it, then timed adding Crumble as a fourth behavior on top. **It took 1 minute 28 seconds and passed on the first run.** The change was one new file, `CrumblePlatformSystem.cs`, plus one registration line in `GameplaySystemsInstaller`. No prefab forked, and `PlayerMotorSimulation`, `MovingPlatformSystem`, `PlatformRiderSystem`, and every platform component were untouched.

That figure is agent-assisted time on an already-established contract, not a from-scratch human estimate, and `CrumbleStateComponent` and `CrumblePlatformBaker` already existed from the blockout task. The honest claim is about *blast radius*, not typing speed: a fourth behavior costs one system and one registration, which is exactly what the design promised.

**Trade-off:** two behaviors can both want to write the platform's pose. I resolved it by precedence rather than by a general arbitration mechanism - a surface that has given way is owned by the crumble system, and `MovingPlatformSystem` yields on any platform that is no longer standable. That is one `if` instead of a scheduler, and it is verified by a test that runs both systems on a single moving-crumble entity.

## 2c. Transform authority on dynamic bodies

Two things wanted to own the pushable crate's root transform: Unity physics through the reference-counted `Rigidbody`, and `EntityTransformComponentListener`, which pushes `EntityTransformComponent` into the root every sync. Left unresolved they fight every frame.

**Physics wins while a dynamic body is simulating, and ECS reads the pose back.** `EntityTransformComponentListener` writes the transform only when the root has no non-kinematic Rigidbody, and `PushableBodySystem` reads position, rotation and velocity back into ECS each fixed tick. ECS stays authoritative for everything kinematic, which is every other entity in the game.

The payoff is that publishing the body's velocity as `PlatformSurfaceComponent.SurfaceVelocity` makes the crate a ride surface for free: it reaches the player through the same rider channel as a moving platform, and jumping off it inherits velocity by the same rule, with no crate-specific code in the motor.

**Trade-off:** a checkpoint restore has to teleport the crate rather than assign its pose, so `RigidBodyService.Teleport` is the single authorised override. Physics being authoritative also means the crate's pose lags ECS by one tick, which is invisible at 60 Hz and is the price of not having two writers.

## 2d. Feature-owned presentation

Player animation and landing feedback live in Player; enemy charge and defeat feedback live in Enemies; coin, platform and checkpoint views live beside their own gameplay components. The model prefabs carry these feature views. The shared Presentation layer binds their entity, blends animations, applies material output and manages pooled model lifetime, without inspecting gameplay-specific components.

Course owns the visual catalog instance, atmosphere and course-wide authoring tools. Arena UI owns HUD authoring. The shared particle service emits a generic clear event; the platform-owned weather notice subscribes to it. The trade-off is explicit prefab binding for each visual feature, checked by `NumTalk/Verify Feature Presentation`, instead of one central class that knows every mechanic. Moves preserve the original asset GUIDs and addressable listener names.

## 3. GUI/UI architecture - MVP

I still use **MVP** for UI. Views render the HUD and menus, presenters translate game state into display state, and ECS remains focused on the fixed-step gameplay simulation.

**Trade-off:** there are two architectural styles in the project, but each is used where it fits: ECS for gameplay and MVP for presentation.

## AI collaboration

I found AI assistance easier to review with data-oriented ECS than with a large OOP hierarchy. Requests are smaller and clearer - add data, update one system, or add one conversion - which reduces overlapping responsibilities and makes features easier to add or remove. This still requires line-by-line review and device testing.

## Controller next

After the core is reliable, I would add camera occlusion handling, remove remaining hot-path allocations, and explore deterministic replay.

## AI-use record - complete before delivery

- Generated versus written/substantially reworked code: **TBD - record honestly during implementation.**
- One assistant error and how I caught it: **TBD - record a real example.**
- One rejected assistant suggestion and why: **TBD - record a real example.**
