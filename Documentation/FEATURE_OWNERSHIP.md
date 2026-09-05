# Presentation feature ownership

Feature behavior lives beside the gameplay component it reads. `Presentation` contains shared rendering machinery; it does not branch on player, enemy, pickup, platform, or checkpoint state.

Paths below are relative to `NumTalkClient/Assets/Project/GameDomain/`.

| Owner | Presentation responsibility | Art and tools |
| --- | --- | --- |
| `Features/Player` | `PlayerModelPresentation`: visual interpolation, facing, animation selection, landing/stomp stretch, hit feedback | `Art/Player.prefab` |
| `Features/Enemies` | `EnemyModelPresentation`: patrol animation, facing, shooter charge, defeat burst | `Art/Patrol.prefab`, `Art/Shooter.prefab` |
| `Features/Pickup` | `CoinModelPresentation`: coin rotation, bobbing and collection burst | `Art/Coin.prefab` |
| `Features/Platforms` | `CrumbleModelPresentation`, `FlashFreezeModelPresentation`, `FlashFreezeNotice`: warnings, frost and countdown | Grass/ice/moving/crumble art and materials; `Editor/FlashFreezeVerification` |
| `Features/Checkpoints` | `CheckpointModelPresentation`: activated marker glow | `Art/Checkpoint.prefab` |
| `Features/Pushables` | Crate art; existing physics remains authoritative | `Art/Crate.prefab` |
| `Features/Goal` | Finish arch art | `Art/Goal.prefab` |
| `Features/Course` | Course atmosphere and composition of feature art | `Data/CourseVisualCatalog.asset`; editor builder, preview shortcuts and integration verification |
| `ScreensDomain/ArenaDomain/Features/Ui` | HUD and its authoring tools | `Art/Heart.png`, `Editor/ArenaHudBuilder` |
| `Features/Presentation` | Shape/pose bridge, catalog schema, model instance pooling, animation blending, material output and shared particle service | Shared materials and Kenney license |

## Runtime contract

A model prefab carries its owning `ModelPresentationFeature` components. The shared `CourseModelPresentation` binds them to the entity, gives them a `ModelPresentationFrame`, blends their requested animation and applies their material output. Feature components may change the visual child only. Their reset and release hooks are called with the pooled model's lifetime.

Platform art carries both crumble and freeze presentation components, so the two behaviors still compose on one entity. The weather notice owns weather formatting and subscribes to the shared particle service's cleared event; the particle service has no platform dependency.

All moved assets retain their `.meta` GUIDs. Addressable listener names are unchanged. `NumTalk → Update Feature Presentation Bindings` refreshes feature components and the weather notice without rebuilding scene geometry. `NumTalk → Apply Kenney Visual Pass` writes new art into the same owning feature folders.

## Verification

`NumTalk → Verify Feature Presentation` covers owning prefab bindings, player visual interpolation without moving collision, animation selection, landing/reset behavior, composed frozen/crumble appearance, shooter charge, coin rotation and checkpoint tint. It is also called by `NumTalk → Verify Priority B`.

The existing motor, camera, platform, layer, crate, enemy, stomp, course and restart verification suites were rerun after the refactor. This structural change does not replace the project's outstanding Android device checks.
