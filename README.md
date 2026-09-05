# Skybound — NumTalk Platformer

Mobile-first 3D platformer prototype for the NumTalk Unity Developer Assignment.

The Kenney visual pass adds floating islands, animated characters, feedback, and a telegraphed flash-freeze event. See [presentation notes and verification](Documentation/Polish/README.md) and [art provenance](ASSET_SOURCES.md).

## Project

| Item | Value |
| --- | --- |
| Unity | `6000.5.0f1` |
| Pipeline | URP |
| Target | Android |
| Input | Unity Input System - floating left stick and right-side variable-height jump |

## Run

1. Open the project with Unity `6000.5.0f1`.
2. Allow package restoration and asset import to finish.
3. Open `Assets/Project/EntryDomain/Scenes/EntryScene.unity` and press Play.
4. On device, use the left thumb to move and the right thumb to jump; both controls work at the same time.

## In scope

- One original 60-120 second 3D course ending at a goal.
- Camera-relative movement with acceleration, deceleration, variable jump, coyote time, and jump buffering.
- Moving, ice, and crumble platforms; a physical ridable crate; stompable patrol enemies; shooter knockback; coins; checkpoints; three lives; and clean restart.
- Fixed-step Arch ECS gameplay with Unity scene objects retained for editable level authoring.
- Android build and a real-device recording with both thumbs visible.

## Out of scope

- More than one course, settings, save data, leaderboards, analytics, ads, or expanded menus.
- Custom shaders, extensive art production, audio production, and non-gameplay UI polish.
- A third-party platformer controller. The controller is implemented for this prototype.

## Known issues

- The prototype is under active implementation; the final Android build and device recording are pending.
- Tuning values and measured jump limits below are placeholders until verified in-game and on device.

## Tuned movement

| Value | Final value |
| --- | --- |
| Move speed | TBD - device tuning |
| Acceleration / deceleration | TBD - device tuning |
| Jump height / max jump distance | TBD - measure in scene |
| Gravity up / down | TBD - device tuning |
| Coyote time / jump buffer | TBD - late/early jump test |
| Ice friction / crumble delay | TBD - platform test |
| Crate mass | TBD - push and ride test |
| Knockback impulse / decay | TBD - control-retention test |

## Layer layout

| Layer | Collides with |
| --- | --- |
| `Player` | Ground, Platform, Pushable, Enemy, EnemyProjectile, Pickup, KillZone |
| `Ground` / `Platform` | Physical gameplay layers |
| `Pushable` | Player, Ground/Platform, EnemyProjectile |
| `Enemy` | Player, Ground/Platform, Pushable |
| `EnemyProjectile` | Player, Ground/Platform, Pushable |
| `Pickup` / `KillZone` | Player only |
| `CameraProbe` | Ground/Platform only |

`EnemyProjectile` does not collide with enemies or other projectiles. Collision decisions use this matrix and explicit layer masks, not tags.
