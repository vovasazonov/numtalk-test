# Engineering Decisions

## 1. Gameplay architecture - ECS

I reused my existing ECS architecture for gameplay. Data and focused systems keep mechanics composable. UI uses MVP.

## 2. Scene authoring - pure ECS conversion, authoring-only scene

`ArenaScene` is used for level authoring. Bakers convert its objects into ECS data and remove the authored GameObjects at runtime. The view pipeline rebuilds the Unity representation from that data, keeping ECS as the game state.

## 3. GUI/UI architecture - MVP

Views display the HUD and menus; presenters translate game state for them. ECS handles gameplay simulation.

## AI collaboration

I had previously developed the project foundation—the core, screens, domains, and ECS architecture—and reused it for this assignment. With AI, I developed the platformer, including its gameplay, art integration, and audio. All folders under `Features/` were developed by AI. I directed the AI to preserve the architectural decisions I specified.

The game was initially not visible on Android. I found the device logs myself and sent the Android errors to AI to help identify the issue. I then resolved it with AI assistance.

Visuals and audio were especially important to me. I rejected performance checks because the game is small and those checks would take too much time. All Priority C items in [PLAN.md](PLAN.md) remain unimplemented: camera occlusion handling, an extra movement ability, zero-allocation profiling, and deterministic replay.
