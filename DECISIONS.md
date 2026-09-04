# Engineering Decisions

## 1. Gameplay architecture - ECS

I considered ECS, MVC, and MVP for the whole project. I chose **ECS with data-oriented design** for gameplay because this platformer has many independent mechanics that must compose: movement, jump forgiveness, moving/ice/crumble platforms, crate physics, enemies, knockback, pickups, and checkpoints. A feature is data plus a focused system, so it is easier to add or remove without changing unrelated behavior.

**Trade-off:** ECS needs a small bridge to Unity objects and is less convenient for presentation-heavy flows. I accept that cost because the game is simulation-heavy.

## 2. Scene authoring - retained Unity objects

Gameplay state is stored in Arch ECS, but level GameObjects remain visible and editable in Unity through retained `SyncWithEntity` baking. This keeps colliders, renderers, and semantic authoring components on the objects a designer works with.

**Trade-off:** the ECS/Unity bridge must stay deliberately small and explicit; it prevents a duplicate runtime view and keeps the scene inspectable.

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
