# Art sources

## Kenney Platformer Kit 4.1

- Creator: Kenney.
- Source: https://kenney.nl/assets/platformer-kit
- License: Creative Commons Zero (CC0), as provided in the supplied archive.
- Supplied by the project owner; inspected and integrated on **2026-09-05**. The original download date was not supplied.
- Original license: `NumTalkClient/Assets/kenney_platformer-kit/License.txt`.
- License copy beside derived art: `NumTalkClient/Assets/Project/GameDomain/Features/Presentation/Art/License-Kenney.txt`.
- The owner's existing `Assets/kenney_platformer-kit` folder is retained to preserve its Unity GUIDs. It is not moved or duplicated into a second third-party folder.

### Files used

All source models below are under `Assets/kenney_platformer-kit/Models/FBX format/`.

| Source file | Use |
| --- | --- |
| `block-grass-large.fbx` | Stable course islands and distant decorative islands |
| `block-snow-large.fbx` | Ice surfaces, including the moving ice platform |
| `block-moving.fbx` | Moving ferry and amber crumble platforms |
| `crate.fbx` | Existing pushable crate's visual child |
| `character-oobi.fbx` | Player, with idle / walk / jump / fall clips |
| `character-oodi.fbx` | Patrol enemies, with the same four-clip presentation set |
| `character-oozi.fbx` | Shooter, with the same four-clip presentation set |
| `coin-gold.fbx` | Spinning pickups |
| `flag.fbx` | Checkpoint markers |
| `door-large-open.fbx` | Finish arch |
| `tree-pine-small.fbx` | Course-edge decoration |
| `flowers.fbx` | Course-edge decoration |
| `rocks.fbx` | Island undersides |
| `tree.fbx` | Distant-island decoration |
| `tree-pine.fbx` | Distant-island decoration |
| `Textures/colormap.png` | Shared Kenney color atlas |

Only these source dependencies are referenced by the new course art. The OBJ, GLB, preview images and unused FBX files remain in the supplied pack but are not referenced by the new visual catalog.

### Project-authored work

Derived art prefabs live in their owning feature folders (Player, Enemies, Platforms, Pickup, Checkpoints, Pushables and Goal). The shared atlas and license stay in Presentation. See [feature ownership](Documentation/FEATURE_OWNERSHIP.md) for the code and tool layout. Asset moves preserve their Unity GUIDs.

The course layout, gameplay controller, ECS systems, primitive collision, normalized visual prefabs, URP material settings, freeze overlay, bounded particle effects, HUD layout and procedural heart icon are authored in this project. No third-party controller, code package or copied course layout is used. The builder is available at **NumTalk → Apply Kenney Visual Pass** in the Unity Editor.

## Audio (CC0)

Downloaded and license-checked on **2026-09-05**. Only one music loop and three short effects are included under `Features/Audio/Art`.

- **Flowerbed Fields [Loop]**, Zane Little Music: [creator's release](https://opengameart.org/content/flowerbed-fields-loop), [original OGG](https://opengameart.org/sites/default/files/flowerbed_fields.ogg). CC0 1.0. Renamed `FlowerbedFields.ogg`, audio unchanged. Streaming at low volume in the arena.
- **Interface Sounds 1.0**, Kenney: [official source and license](https://kenney.nl/assets/interface-sounds). CC0. `glass_001.ogg` → `Coin.ogg`; `confirmation_002.ogg` → `Confirm.ogg` (checkpoint and finish); `error_006.ogg` → `LifeLost.ogg`. Audio unchanged.
- Source/license notices are included beside the audio. No account, subscription, or paid assets are required.
