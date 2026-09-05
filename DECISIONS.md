# Engineering Decisions

## 1. Gameplay architecture - ECS

I considered ECS, MVC, and MVP for the whole project. I chose **ECS with data-oriented design** for gameplay because this platformer has many independent mechanics that must compose: movement, jump forgiveness, moving/ice/crumble platforms, crate physics, enemies, knockback, pickups, and checkpoints. A feature is data plus a focused system, so it is easier to add or remove without changing unrelated behavior.

**Trade-off:** ECS needs a small bridge to Unity objects and is less convenient for presentation-heavy flows. I accept that cost because the game is simulation-heavy.

## 2. Scene authoring - pure ECS conversion, authoring-only scene

`ArenaScene` is an authoring artifact. Artists lay the course out there with real meshes, colliders and semantic bakers, and every baker serialises what it needs into plain ECS data. At runtime the bake is `ConvertAndDestroy`: the authored GameObjects are gone and the Arch world is the only game state.

The runtime representation is rebuilt from that data by the existing view pipeline. `ViewSystem` spawns one `EntityView` root per entity, and each ECS component that needs a view gets its own `ComponentListener` child. Unity components that must sit on the root - `Rigidbody`, `CharacterController` - are declared by the listeners that need them and reference-counted by `EntityView`: added on first request, destroyed when the last requiring ECS component is removed. So one crate carrying both `PushableComponent` and `PhysicsBodyComponent` gets exactly one Rigidbody, and it survives losing either component alone.

**Trade-off:** the authored look must survive as data, so shape, size, colour and collider volume are baked into components rather than kept as object references. The course is built from Unity primitives, which the brief requires anyway, so this is cheap. The cost is one extra GameObject per viewed component and an addressable load per listener type, paid once at level load.

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
