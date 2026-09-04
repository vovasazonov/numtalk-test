---
name: numtalk-ecs
description: Implement or modify ECS features in NumTalkClient's ArenaDomain, including Arch systems, feature installers, authoring bakers, and ECS-driven views. Use for gameplay work under Assets/Project/GameDomain/Features.
---

# NumTalk ECS

Use this skill when working on gameplay in the NumTalkClient Unity project. Paths below are relative to the repository root.

## Architecture to preserve

Gameplay runs in an `Arch.Core.World` owned by `ArenaScreenScope`, not in scene `MonoBehaviour` logic.

```text
ArenaScreenScope
  -> creates one Arch world and registers ECS systems through VContainer
  -> initializes ComponentListenerRegistry
  -> loads ArenaScene additively
  -> BakerComponent converts authoring GameObjects into entities, then destroys them
  -> systems update ECS state
  -> ViewSystem creates Entity.prefab views for entities with ViewComponent
  -> ComponentListeners mirror selected ECS components onto those views
```

The Arena scope is in `Assets/Project/GameDomain/ScreensDomain/ArenaDomain/Scripts/ArenaScreenScope.cs`. It is the composition root for arena gameplay. Add an installer there only when the feature must run in every arena session.

`EcsArchitectureInstaller` creates the world via `UseNewArchApp`, registers `ComponentListenerRegistry`, and registers `ViewSystem`. Systems are registered with `builder.RegisterSystemIntoArchApp<TSystem>()`; do not create gameplay systems manually.

Use constructor injection for system dependencies. A system derives from `Arch.Unity.Toolkit.UnitySystemBase`, receives `World` in its constructor, and overrides `Initialize`, `BeforeUpdate`, `Update`, or `Dispose` only when needed.

## The feature contract

Keep a feature self-contained under `Assets/Project/GameDomain/Features/<FeatureName>/`.

- Component: a small `struct` that holds ECS data. Put it in the feature's `Scripts` folder.
- System: a `UnitySystemBase` that owns behavior. Define cached `QueryDescription` and delegate fields once; query the world in `Update`.
- Installer: a static `<Feature>Installer` that registers the system(s) with `RegisterSystemIntoArchApp`. Call it from `ArenaScreenScope` if it is an arena-wide feature.
- Baker: an authoring `MonoBehaviour` in the folder that owns the component. It implements `Arch.Unity.Conversion.IComponentConverter` and adds exactly its feature's component(s).
- Listener: add one only when a component needs a Unity view. It derives from `ComponentListener<TComponent>` and updates a pooled child of the entity view.

Feature systems should communicate through components, not direct calls to another system. A short-lived event is also a component: add it after collecting entities, consume it in the intended system, and remove it deliberately. `JumpRequestComponent` and `PickUpEventComponent` are the current examples.

When a query can add, remove, or destroy entities, first collect the target entities and perform structural changes after the query. `JumpSystem`, `PickUpCollisionSystem`, `ReaperSystem`, and `ReapBehindPlayerSystem` follow this rule.

## Arena scene and baking

`ArenaSceneLoader` loads `ArenaScene.unity` additively and scans every root GameObject for `BakerComponent` descendants. `BakerComponent.Bake(World)` calls Arch's conversion in `ConvertAndDestroy` mode:

1. The target GameObject's `IComponentConverter` bakers contribute their ECS components.
2. An entity is created in the injected Arena world.
3. The authoring GameObject, including the root baker and its feature bakers, is destroyed.

This makes the scene an authoring source, not a hybrid runtime hierarchy. Do not put ongoing gameplay behavior in authoring `MonoBehaviour`s; place it in systems and ECS components.

To author a new scene entity:

1. Put one `BakerComponent` on the GameObject that represents one entity.
2. Add feature-specific bakers to that same GameObject. Each baker must live in the feature that owns the data it writes.
3. Add `ViewComponent` through a baker when the entity needs a rendered Unity view.
4. Place the prefab in `ArenaScene.unity`, and include a new scene in build settings if a loader must load it by path.

The current `Features/Player/Prefabs/Player.prefab` is the reference entity. `PlayerBaker` adds `ViewComponent`, `PlayerTagComponent`, and `PickUpCollectorComponent`; Position, Movement, Creature, Physics, and Gizmos bakers each add their own feature's data. `PlayerSpawnSystem` remains as legacy code but is intentionally not registered: do not re-register it while the Arena scene owns player authoring, or the game will create a duplicate player.

## ECS-to-Unity view bridge

An entity is invisible to Unity until it has `ViewComponent`. `ViewSystem` then loads `Entity.prefab`, pools it, and gives it an `EntityView` tied to the entity.

`ComponentListenerRegistry` reflects over every non-abstract `ComponentListener` class in the game assembly. For each listener type it loads an Addressable whose address is exactly:

```text
<ListenerClassName>.prefab
```

For example, `PositionComponentListener` requires the Addressable `PositionComponentListener.prefab`. Adding a listener without creating and marking its matching prefab as Addressable will fail during arena initialization. Keep listener prefabs in the owning feature's `Prefabs` folder and retain the exact class/file/address name convention.

The current bridge has these responsibilities:

- `PositionComponentListener`: puts the entity-view parent at `PositionComponent.Position`.
- `CreatureComponentListener`: chooses and animates sprite and shadow frames from `CreatureStateConfigDatabase`.
- `LocationComponentListener`: instantiates the configured location tileset under the entity view.
- `JoystickComponentListener`: displays the dynamic joystick in screen-derived world coordinates.
- `GizmosComponentListener`: draws runtime ECS gizmos in the editor.

Listeners are presentation only. They must not become the gameplay source of truth. A listener can be pooled and disabled at any time, so reset transient Unity state in `OnDisable` when necessary.

## Current feature map

### Foundation

- **EcsArchitecture**: creates the Arch app/world, manages entity views and listener pooling, and provides the root `BakerComponent`.
- **Configs**: `IConfigService.Get<T>()` loads JSON from `Features/Configs/Resources`. A config type can use `[ConfigKey("...")]`; otherwise its class name is the resource key.
- **Universe**: `UniverseConsts.PixelsPerUnit` is `32`; use `CalculateUnitsBasePixels` for gameplay distances derived from art pixels.

### Position, movement, and physics

- **Position** owns `PositionComponent { float3 Position }` and `PositionBaker`, which reads the authoring transform position. Position is the canonical world-space location for ECS entities.
- **Movement** owns `MovementComponent { float3 Velocity }`, `MovementBaker`, and `MovementSystem`, which integrates `Position += Velocity * deltaTime`.
- **Physics** owns `PhysicsComponent` (the gravity source), `RigidbodyComponent`, `ColliderComponent`, and `FallingComponent`. `GravitySystem` keeps gravity-enabled bodies above ground; `FallSystem` applies vertical velocity while a body has `FallingComponent`. Collider height establishes the resting Z position. Use the Physics bakers for authoring collider size and gravity behavior.
- **Jump** consumes `JumpRequestComponent`. It requires Position, Movement, Collider, and Rigidbody on the requester, turns off direct gravity, and adds `FallingComponent`. `FallSystem` restores gravity when the body lands.

### Input and player

- **Input** reads Unity Input System pointer data. It maintains singleton-style `PointerPressComponent` and `JoystickComponent` entities; pointer taps over UI are deliberately excluded.
- **GameInput** transforms raw input into transient `MoveInputComponent` and `JumpInputComponent` entities. Keyboard arrows and the virtual joystick feed movement; a UI-safe tap or Space feeds jump.
- **Player** identifies controllable entities with `PlayerTagComponent`. `PlayerMoveSystem` reads `MoveInputComponent`, `LocationComponent`, `MovementComponent`, and `PositionComponent`; it uses `PlayerConfig` from `player.json`. `PlayerJumpSystem` converts jump input into requests. `CameraFollowPlayerSystem` follows player position on the X axis.

### Creature rendering and state

**Creature** is both gameplay state and the animated visual contract. `CreatureComponent` contains `CreatureType`, `CreatureState`, `CreatureSide`, and height above ground.

`CreatureStateSystem` derives state each update:

- vertical position plus collider size determines airborne height;
- movement velocity determines `Idle` versus `Walk` and facing direction;
- airborne non-hovering entities become `Jump`.

`CreatureComponentListener` renders that state from `CreatureStateConfigDatabase`. Sprite data lives in `Features/Creature/Data`; each `CreatureStateConfig` maps type + state + direction to body and shadow frame arrays. For new creature visuals, update the matching `CreatureType`, state config asset/database, and listener prefab data together. Do not set Unity sprites from gameplay systems.

### World, pickups, and lifetime

- **Location** is installed through the serialized `LocationInstaller` on the Arena screen prefab, before the code-based arena installers. It persists the selected location, reads gravity from `locations.json`, and creates repeating location tile entities with View, Location, Position, and Physics components. Its listener instantiates the visual tileset.
- **Pickup** uses marker components. A collector needs `PickUpCollectorComponent`, Position, and Collider; an item needs `PickUpAbleComponent`, Position, and Collider. Collision removes the item marker and adds a one-update `PickUpEventComponent` containing the collector entity. The reset system removes prior events in `BeforeUpdate`.
- **Reaper** destroys entities once `ReaperComponent.TimeRemaining` reaches zero.
- **ReapBehindPlayer** adds `ReaperComponent` to entities carrying `ReapBehindPlayerComponent` once they fall 400 art pixels behind the player. Add a non-zero lifetime when an entity should survive briefly after crossing that threshold.
- **Gizmoses** owns `GizmosComponent` and its listener. `GizmosBaker` previews the same shape, size, color, and wireframe state in the Scene view that it writes to ECS.

## Adding a feature safely

Before coding, identify the components that produce the data, the systems that consume it, and whether the entity needs a view. Then make the smallest coherent change:

1. Add the component struct in its owning feature.
2. Add a system with explicit `WithAll`/`WithNone` queries and constructor-injected services.
3. Create or extend the feature installer; register it from `ArenaScreenScope` if it should be active in Arena.
4. Add a baker in that feature if artists must author the data in a scene or prefab.
5. Add `ViewComponent` only for entities that need Unity presentation.
6. If presentation is new, add `XComponentListener`, its matching Addressable `XComponentListener.prefab`, and reset behavior for pooling.
7. Update the scene/prefab and any ScriptableObject, JSON, localization, or Addressables data that the feature requires.
8. Check for duplicate entities and for all component prerequisites of downstream queries.

Do not rename serialized baker/listener types, prefab addresses, or asset data types casually. Those names connect Unity serialized references, dynamic listener discovery, and Addressables. When a rename is necessary, migrate all related prefab, address, and asset references in the same change.

## Review checklist

- New gameplay state is an ECS component, not a `MonoBehaviour` runtime field.
- Every system is registered exactly once in the Arena world.
- Structural changes are deferred until after the query that discovered targets.
- Authoring data is owned by a feature baker on the source GameObject.
- An entity that should render has `ViewComponent` and its visual components have matching listener prefabs.
- A new listener's prefab address is exactly `<ListenerClassName>.prefab` and is Addressable.
- Components required by downstream systems are present on baked entities.
- Player creation remains scene-baked; do not reactivate the legacy spawn registration.
