# NumTalk 3D Platformer - Implementation Plan

## 1. Outcome and non-negotiables

Build one original, mobile-first 3D platformer course that takes 60-120 seconds to complete when played well. The submission should make the player feel in control: responsive camera-relative movement, a forgiving-but-legible jump, reliable contacts, and every platform interaction composing predictably.

The source of truth is `NumTalk_Unity_Developer_Assignment.pdf`. This plan covers every **MUST** item, the three **SHOULD** items, the requested polished scene workflow, and schedules the bonus items only after the core has passed its verification gates.

- Unity version: keep the installed Unity `6000.5.0f1`; record it in the final README.
- Input: use the already-installed Input System package, never keyboard-only controls. The test target is two simultaneous thumbs on an Android device.
- Art: use an original course layout and our own names. Do not use Nintendo assets, characters, music, or copied level layouts.
- Scope discipline: menus remain a start/game-over overlay, with only a coin counter and three life pips. Gameplay, collision, device feel, and documentation take precedence.
- Commit in small, reviewable slices. The current repository has only an initial commit, so the history must demonstrate the engineering decisions rather than end with one bulk commit.

## 2. Project assessment and the architectural decision

The repository is a Unity 6 project with Arch ECS, VContainer, UniTask, URP, and the Input System already installed. The current `ArenaScene` is loaded additively by `ArenaSceneLoader`, then its `BakerComponent`s are converted into the VContainer-owned Arch `World`.

The existing `BakerComponent` uses `ConversionMode.ConvertAndDestroy`. That is appropriate for the existing runtime-view pipeline, but wrong for an authored 3D platformer course: it destroys the scene GameObject and therefore loses the visible collider/renderer/CharacterController that a level designer needs to see and edit.

### Chosen model: pure ECS state, runtime view rebuilt from baked data

`ArenaScene` stays authoring-only and the bake stays `ConvertAndDestroy`. Bakers implement `IComponentConverter` and serialise everything a system or view needs - pose, layer, primitive shape, size, tint, collider volume, body settings - into plain ECS data. No component holds a Unity object reference.

The runtime object is then rebuilt from that data by the existing view pipeline:

- `ViewSystem` spawns one pooled `EntityView` root per entity carrying `ViewComponent`.
- Each ECS component that needs a view gets its own `ComponentListener` child, addressable-loaded by type name.
- Unity components that must live on the root are declared per listener via `RequiredRootComponents` and reference-counted by `EntityView`. The root adds one on first request and destroys it when the last requiring ECS component is removed, so several components can share one `Rigidbody`.

This gives us both things we need:

- **At edit time:** the hierarchy reads like a real level, with actual meshes, colliders, materials, and bakers visible on the object they describe.
- **At runtime:** Arch components are the sole authority. Unity physics is a service the view layer assembles on demand from that data.

Systems must never reach for a Unity object through `GetComponent`; they read and write ECS data, and listeners project it.

### System scheduling

Arch.Unity exposes `SystemRunner.FixedUpdate`, which uses `Time.fixedDeltaTime`. Register motor, platform motion, collision resolution, projectile simulation, stomp, and checkpoint state systems in this runner, in intentional order. Register input sampling and presentation-only systems in `Update`/`PreLateUpdate`; run camera and visual interpolation in `LateUpdate` or an equivalent presentation bridge.

The fixed simulation runs at 60 Hz. Input is sampled every render frame and edge-latched until the next simulation tick, so a short jump tap is not lost at 30 FPS. Every gameplay timer and decay uses fixed delta time rather than render delta time.

## 3. Authoring-visible scene design

Keep the existing screen flow, but replace the old 2D arena content with a 3D authored course in `Assets/Project/GameDomain/ScreensDomain/ArenaDomain/Scenes/ArenaScene.unity` (or a clearly named additive `PlatformerCourseScene` loaded by the same screen loader). There must be one playable scene in Build Settings.

Create this hierarchy and keep it clean enough to inspect during the interview:

```text
ArenaScene
|- CourseRoot
|  |- StaticPlatforms
|  |- MovingPlatforms
|  |- IcePlatforms
|  |- CrumblePlatforms
|  |- Pushables
|  |- Enemies
|  |- Pickups
|  |- Checkpoints
|  |- Hazards
|  `- Goal
|- PlayerStart
|- CameraRig
|- LightingAndBackdrop
`- MobileHud
```

Every interactive object gets `BakerComponent` plus a semantic baker. Its Inspector fields are the source of the initial ECS component values. Authoring components use the project's existing `XxxBaker` naming rather than `XxxAuthoring`, and the `CharacterController` / `Rigidbody` bridges are `[RequireComponent]` dependencies of the semantic baker rather than separate components to add by hand:

| Scene object | Prefab | Baker component(s) | ECS data written by the baker |
| --- | --- | --- | --- |
| Player capsule | `PlatformerPlayer` | `PlayerBaker`, `CharacterBodyBaker` | `PlayerTag`, `InitialState`, `PlayerMotor`, `JumpState`, `GroundState`, `ExternalVelocity`, `PlatformRider`, `Health`, `CheckpointReference`, `CharacterBody` |
| Static cube/platform | `StaticPlatform` | `PlatformBaker` | `PlatformSurface`, `InitialState` |
| Moving platform | `MovingPlatform` | `PlatformBaker`, `MovingPlatformBaker` | + `PlatformMotion` |
| Ice platform | `IcePlatform` | `PlatformBaker`, `IceSurfaceBaker` | + `IceSurface` |
| Moving ice platform | `MovingIcePlatform` | `PlatformBaker`, `MovingPlatformBaker`, `IceSurfaceBaker` | + `PlatformMotion`, `IceSurface` |
| Crumble platform | `CrumblePlatform` | `PlatformBaker`, `CrumblePlatformBaker` | + `CrumbleState` |
| Crate | `PushableCrate` | `PushableCrateBaker`, `PhysicsBodyBaker` | `Pushable`, `PlatformSurface`, `InitialState`, `PhysicsBody` |
| Patrol/stomp enemy | `PatrolEnemy` | `EnemyBaker`, `PatrolBaker` | `Enemy`, `StompTarget`, `InitialState`, `Patrol` |
| Shooter | `ShooterEnemy` | `EnemyBaker`, `ShooterBaker` | `Enemy`, `InitialState`, `Shooter` |
| Coin | `Coin` | `CoinBaker` | `Pickup` (stable id), `InitialState` |
| Checkpoint | `Checkpoint` | `CheckpointBaker` | `Checkpoint` (ordered id, respawn point) |
| Kill zone | `KillZone` | `KillZoneBaker` | `KillZone` |
| Goal | `Goal` | `GoalBaker` | `Goal` |

Every object also carries `EntityTransformBaker` (pose, layer, `ViewComponent`), `ShapeBaker` (primitive, size, tint) and, where it has a collision volume, `PhysicsColliderBaker`. Those three are what let the runtime view rebuild the authored object after the scene GameObject is destroyed.

The platform behavior model is intentionally compositional: `PlatformSurface` is the shared base data, while `MovingPlatform`, `IceSurface`, and `CrumbleState` are independent components on the same entity. No forked Moving/Ice/Crumble prefab families. Adding a fourth behavior later should be one authoring component, one ECS component, and one system - not a rewrite.

## 4. One tuning asset, measurable movement

Create one `PlatformerTuning` ScriptableObject under the `Configs` feature folder. All player, platform, crate, camera, combat, and feedback values live there, are labelled with units, and are referenced by the level authoring components/systems. No magic numbers in the motor.

Initial values are deliberately starting points, not final claims. Tune them on a device and publish the final values and measured maximums in the README.

| Group | Starting values to tune |
| --- | --- |
| Run | 7.5 m/s top speed; 60 m/s2 ground acceleration; 70 m/s2 ground deceleration; 55% air acceleration |
| Jump | 2.6 m target apex; stronger fall gravity (about 1.7x ascent); 0.45 early-release velocity cut; 32 m/s terminal speed |
| Forgiveness | 0.11 s coyote time; 0.14 s jump buffer |
| Platform | 0.10 ice deceleration scale; 0.35 s crumble warning; 0.55 s crumble delay; 3.0 s respawn |
| Combat | 9 m/s knockback; 0.28 s airborne / 0.16 s grounded impulse half-life; 6.5 m/s stomp bounce and a higher held-jump bounce |
| Crate | 6 kg mass, high enough push force to move it with intent but not make it skate on normal ground |

Measure maximum jump height and horizontal distance in the actual scene with a level ruler, after tuning. Build every required gap at no more than 75% of the measured horizontal maximum so the course tests decisions rather than pixel-perfect execution.

### Player motor

Use a `CharacterController` driven by our own ECS velocity model. It provides a swept character move and controlled slope/step behavior without adopting a third-party controller. The player has three velocity channels, summed only when issuing `CharacterController.Move`:

1. **Intrinsic velocity**: camera-relative thumb intent, acceleration/deceleration, gravity, and variable jump.
2. **External velocity**: projectile knockback and other impulses, independently decayed with exponential half-life. Input remains fully responsive while the impulse is visible.
3. **Platform velocity**: position/rotation delta inherited from the standing platform. On jump, preserve horizontal velocity and rising vertical velocity only, with a sensible clamp.

Grounding uses a bounded capsule/sphere probe and the controller's move flags, not a single `isGrounded` boolean. The motor supports full but reduced air steering, jump cutting on release, coyote time, and a buffered press consumed on landing. A presentation child interpolates between fixed poses; it does not change collision state.

### Camera and touch controls

Implement a floating left virtual stick: the first touch in the left region establishes its center and its drag supplies camera-relative movement. A touch in the right region queues jump; release controls variable height. Both pointers must operate concurrently.

The third-person camera uses a damping follow target, a small velocity lead, and a ground-reference vertical target while the player is airborne, so it does not lurch upward with a jump. Compose the course and baseline framing so the next landing is visible before commitment; the explicit camera-occlusion spherecast/pull-in solution is a Priority C bonus. Keep camera touch/orbit optional only if it does not compromise reliable right-region jump input.

## 5. Collision contract - implement and test before decorating

This is the first graded block. Define named layers once in Project Settings and use the collision matrix plus explicit `LayerMask` fields for every cast. Never depend on `CompareTag` inside collision callbacks.

| Layer | Collides with | Purpose |
| --- | --- | --- |
| `Player` | `Ground`, `Platform`, `Pushable`, `Enemy`, `EnemyProjectile`, `Pickup`, `KillZone` | Character and sensors |
| `Ground` / `Platform` | all physical gameplay layers | Course geometry and platform surfaces |
| `Pushable` | player, ground/platform, enemy projectile | Dynamic crate, continuous collision detection |
| `Enemy` | player, ground/platform, pushable | Patrol and shooter bodies; never own projectile |
| `EnemyProjectile` | player, ground/platform, pushable | Spherecast target mask; never enemy/projectile |
| `Pickup` / `KillZone` | player only | Trigger-only interactions |
| `CameraProbe` | ground/platform | Camera occlusion mask only |

### Pushable crate

Use a real Rigidbody crate with a visible cube mesh, mass, friction material, interpolation, and `ContinuousDynamic` collision detection. The player motor reports controller contacts through a bridge; a horizontal, mass-scaled impulse is applied to the crate on valid push contacts. The player receives resistance through its motor rather than being displaced by the crate, preventing wedging and preserving control.

The crate must be a valid ride surface: standing on it supplies surface motion to `PlatformRider`; jumping from it uses the same inheritance rule as a moving platform. Test it against a wall, off an edge, on ice, and while the player is standing on it.

### Enemies, projectiles, stomp, and knockback

- Patrol enemies are stompable targets. Resolve stomp from the player's fixed-step swept capsule/segment, contact normal/height, and downward velocity - not a fragile `OnTriggerEnter` timing event. A valid top-down hit destroys the enemy and bounces the player; a side or underside contact damages the player. Verify both terminal fall speed and forced 30 FPS.
- Shooter projectiles are pooled kinematic objects. Simulate each fixed step with a `SphereCast` over the complete travel segment, then place it at the hit point or end point. This makes high-speed tunneling impossible by design and lets the cast's explicit layer mask exclude the firing enemy and other projectiles.
- A projectile hit adds an external impulse to the player motor. Decay it exponentially, independently from intrinsic movement, so being shot on ice, in air, on a moving platform, or while leaning on the crate remains legible and controllable.

## 6. Platform behavior and checkpoint policy

### Platform components

1. `MovingPlatformSystem` evaluates an authored route/curve in fixed time, applies a delta transform to riders (never player parenting), and exposes current surface velocity.
2. `IceSurfaceSystem` reduces only intrinsic deceleration. The player can still accelerate with intent, but momentum carries them past an edge.
3. `CrumblePlatformSystem` transitions `Stable -> Telegraphing -> Falling -> Respawning`; it has a visible warning and delay before its collider is disabled/falls. It shares the same platform entity as Moving/Ice, proving behaviors are composable.

Create at least one platform carrying both Moving and Ice components. Time the addition of Crumble after the shared surface contract exists and record the actual time in `DECISIONS.md`; this is direct evidence that the design is composable.

### Lives, respawn, and reset

The player starts with three lives. Falling below the kill plane or a valid side/underside enemy hit removes one life, clears external velocity, and respawns at the last activated checkpoint. At zero lives, restart the whole run cleanly. Exercise the third complete restart, not just the first.

At each checkpoint, save a deterministic snapshot of mutable world state: player spawn, crate pose/velocity, platform phases, crumble state, enemy life/patrol/shoot cooldown, and which stable pickup IDs are already collected. On respawn, return projectiles and transient effects to their pools, restore the snapshot, and retain previously collected coins. This avoids duplicate rewards while making the retry fair and coherent. Document this exact policy in the README.

## 7. Course, readability, feel, and art

### Course beat sheet

Author a bright, original low-poly floating-island course. Start from simple primitive blockout so collision, scale, and landing readability are correct before any model swap.

| Beat | What the player learns or proves |
| --- | --- |
| 1. Arrival / gentle gaps | Camera-relative run, landing read, variable jump, early coins |
| 2. Crate landing puzzle | A crate blocks the required landing; it must be pushed, can fall, can be ridden, and later reaches ice |
| 3. Patrol ledges | Clear stomp-from-above versus side-hit feedback |
| 4. Shooter approach | Projectile knockback without a cheap death; clear wind-up and a safe response route |
| 5. Moving ice crossing | Ride a moving platform, preserve jump-off velocity, then manage reduced deceleration |
| 6. Crumble chain / checkpoint | Telegraph, commitment, fair recovery, and a visible checkpoint before the final section |
| 7. Sudden event and final run | A telegraphed flash-freeze changes selected surfaces to ice, followed by a final mixed-mechanic approach to the flag |
| 8. Goal | Clear flag/portal finish and compact game-over/restart flow |

Place coins along safe-to-risky lines rather than as filler. Use color/material language consistently: warm stable platforms, cyan ice, amber flashing crumble, red projectile danger, and a clearly contrasting goal. Add only cheap, high-value feedback after mechanics pass: landing squash/stretch on the visual child, coin pop, stomp bounce pop, platform warning pulse, and shooter wind-up/knockback telegraph.

### Free-art choice and import policy

Use **Kenney Platformer Kit** as the primary optional visual set. It is a 3D kit with 150 assets, animation/variation support, and a CC0 license: [Kenney Platformer Kit](https://kenney.nl/assets/platformer-kit?part=f80a26e0-2528-46c5-9b4b-97fb5870a05b). If a minimal foliage/backdrop pass is useful, use only the compatible CC0 [Kenney Nature Kit](https://kenney.nl/assets/nature-kit). These assets may dress the scene but must never replace collision clarity or import a ready-made controller/code package.

Keep original primitive colliders even when a decorative mesh is added. Import source files into `Assets/ThirdParty/Kenney/` with their license text and an `ASSET_SOURCES.md` ledger containing source URL, download date, license, and exactly which files are used. The submission can then demonstrate compliant, deliberate use of free art without obscuring the geometry the player stands on.

## 8. Ordered implementation checklist

Task status uses standard Markdown checkboxes: change `- [ ]` to `- [x]` when the acceptance condition is genuinely met. Complete tasks in order within a priority. Do not start Priority B until all Priority A verification gates pass; do not start Priority C until A and B pass.

### Priority A - delivery-critical MUST work

- [x] **A1 - Establish the platformer foundation.** Create feature folders, `PlatformerTuningConfig.asset`, physics materials, named layers, and the clean 3D scene hierarchy described above.
- [x] **A2 - NOT RELEVANT ANYMORE
- [x] **A3 - Build the full primitive blockout.** Place player spawn, platforms, gaps, hazards, checkpoints, crate route, enemies, coins, and goal in the Unity Scene view. Every interactive object has its semantic baker/authoring component.
- [ ] **A4 - Establish fixed-step ECS scheduling.** Register simulation systems in `SystemRunner.FixedUpdate`, presentation systems separately, and input edges as latches consumed by the next simulation tick.
- [ ] **A5 - Implement two-thumb mobile input.** Deliver the left floating stick and right jump region with simultaneous pointers; validate it on an Android device before continuing.
- [ ] **A6 - Implement the player motor.** Add camera-relative acceleration/deceleration, the three velocity channels, ground probe, variable jump, jump cut, coyote time, jump buffer, gravity split, terminal speed, and visual interpolation.
- [ ] **A7 - Implement readable third-person camera.** Add damping, velocity lead, ground-reference vertical follow, and baseline course framing that leaves the player and next commitment visible.
- [ ] **A8 - Measure movement and resize the course.** Record preliminary maximum jump height/distance in scene and keep every gap at or below 75% of the measured horizontal limit.
- [ ] **A9 - Enforce the layer collision contract.** Configure the full matrix, explicit masks on casts, and thin-platform player collision test; eliminate tag-based collision decisions.
- [ ] **A10 - Build composable platform behavior.** Implement shared `PlatformSurface`, then Moving, Ice, and Crumble components/systems; prove Moving+Ice on one instance and record the fourth-behavior implementation time.
- [ ] **A11 - Implement the physical, ridable crate.** Use a continuous dynamic Rigidbody with mass/friction, player resistance, safe push impulse, and platform-riding/jump-off inheritance; pass wall, edge, and ice cases.
- [ ] **A12 - Implement enemies, projectiles, and knockback.** Add patrol and shooter authoring, pooled fixed-step SphereCast projectiles, layer filtering, and independently decaying external velocity.
- [ ] **A13 - Implement reliable stomp resolution.** Use fixed-step swept/contact discrimination; top hits bounce and defeat enemies, side/bottom contacts hurt, including at terminal speed and forced 30 FPS.
- [ ] **A14 - Implement lives, checkpoints, and clean reset.** Add three lives, snapshot restore policy, kill zone, zero-life restart, pooled transient cleanup, and a third-restart regression test.
- [ ] **A15 - Integrate the complete core course.** Play the seven course beats end-to-end with all mandatory interactions, a 60-120 second target duration, coin routes, and an unmistakable goal.
- [ ] **A16 - Pass all Priority A verification gates.** Device controls, forced 30/60/120 FPS, no player/projectile tunneling, crate cases, four knockback compositions, platform composition, restart, and retained-scene bake inspection are all green.
- [ ] **A17 - Create the required deliverables.** Finish the one-page README and DECISIONS.md, Android build, and sub-90-second on-device recording with both thumbs visible.

### Priority B - requested polish and SHOULD work

- [ ] **B1 - Add the telegraphed sudden event.** Implement the flash-freeze section that visibly warns, temporarily converts selected surfaces to ice, and gives the player a fair response window.
- [ ] **B2 - Add high-value feel feedback.** Add landing squash/stretch, coin pop, stomp bounce pop, crumble warning, shooter wind-up, and readable knockback without changing collision state.
- [ ] **B3 - Apply the restrained CC0 visual pass.** Import only the selected Kenney models, keep primitive collision volumes, light the original floating-island course, and preserve platform readability on a phone screen.
- [ ] **B4 - Record the art provenance.** Add the Kenney license files and `ASSET_SOURCES.md` with source URL, date, license, and used-file list.
- [ ] **B5 - Re-run the full verification pass.** Confirm B1-B4 did not alter touch input, frame-rate consistency, collision, camera framing, or device performance.

### Priority C - bonus work (only after A and B are stable)

- [ ] **C1 - Add camera occlusion handling.** Spherecast from target to camera and pull in or fade only blocking geometry, without exposing geometry popping during a jump.
- [ ] **C2 - Add one original movement ability.** Prefer a tuned dash with a cooldown because it composes naturally with the existing velocity channels and has a single clear purpose in the course.
- [ ] **C3 - Achieve and evidence zero per-frame GC in the hot path.** Pool gameplay transients, replace allocating queries/casts, profile the movement/combat loop, and save the Profiler evidence.
- [ ] **C4 - Add deterministic replay.** Record fixed-step input, reproduce the last run exactly via a replay button, and document any determinism boundaries.

## 9. Verification gates

Do not move to visual polish while any collision gate fails. Capture evidence as each gate turns green.

- Device: floating stick and jump register independently and simultaneously on Android glass.
- Frame rate: force 30, 60, and 120 FPS; movement, late jump, buffered landing jump, and camera behavior are equivalent.
- Player collision: fall at terminal velocity onto a thin platform ten times; no tunneling.
- Stomp: top/side/underside discrimination passes at terminal velocity and forced 30 FPS.
- Crate: push it against a wall, over an edge, onto ice, stand on it, and jump from it; no penetration or player shove-through.
- Projectile: high-speed projectile cannot hit owner, other projectiles, or tunnel through a platform/player.
- Composition: get shot while airborne, on ice, on a moving platform, and while leaning on the crate; player retains control in all four cases.
- Platform: moving + ice works on one scene instance; crumble adds without a prefab fork.
- Restart: lose all three lives, restart three full runs, and confirm no stale projectiles, coroutines, entities, or broken checkpoint state.
- Scene authoring: inspect the Unity hierarchy and confirm a Player, a moving-ice platform, crate, shooter, checkpoint, and goal retain their semantic authoring component plus a valid `SyncWithEntity` link after bake.
- Performance: use the Unity Profiler on device/editor to check the movement loop. Pool projectiles, enemies, pickups, and effects; remove per-frame allocations before claiming the bonus.

When Unity MCP access is available, use it to verify the loaded scene hierarchy and component assignments, capture a multi-angle scene view, inspect console errors, and collect the profiler evidence. The current MCP connection is revoked in Unity Project Settings, so reconnect it there before these editor-side verification passes.

## 10. Documentation and commit plan

`README.md` (one page) will contain: Unity version; setup/run/device instructions; what is in/out; known issues; final tuning numbers; measured max jump height/distance; checkpoint restore policy; layer matrix; art-source notice; and Android build/recording location.

`DECISIONS.md` (one page) will contain the three hard calls and trade-offs: CharacterController plus ECS velocity channels, retained scene baking with Arch synchronization, and composable platform behavior. It will also state next controller work, the timed fourth-behavior result, and the required honest AI note: approximate generated/reworked share, one assistant error caught in this task, and one rejected suggestion with rationale.

Commit after each meaningful accepted slice, for example:

```text
scaffold: add platformer tuning and layers
ecs: retain scene authoring objects during bake
scene: block out complete course and gameplay markers
motor: fixed-step movement and forgiving jump
input: dual-thumb floating stick controls
camera: lead, damping, and occlusion handling
platforms: compose moving ice and crumble surfaces
collision: add physical crate and layer matrix
combat: spherecast projectiles and external knockback
combat: add swept stomp resolution
run: checkpoints, lives, and clean restart
course: author full mechanic sequence and goal
polish: add feedback and CC0 environment dressing
docs: add README decisions and asset ledger
build: verify Android device recording
```

## 11. Cut order and definition of done

If a real eight-hour submission limit applies, cut in the assignment's order: bonuses, extra feel, sudden event, then course length. Never cut the collision block, mobile device test, documentation, or clean restart. For this requested full implementation, keep the same priority order but complete the SHOULD and bonus items only after the MUST gates remain green.

The project is done when the editable Unity scene, Arch entity state, mobile controls, course, collision tests, documentation, build, and device recording all agree with the plan above - not merely when a capsule can jump on cubes.
