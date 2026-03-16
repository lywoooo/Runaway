# Runaway

Runaway is a compact arcade driving prototype built in Unity. You spawn into a procedurally generated city, thread through tight road networks, avoid police pressure, and reach the destination before the arrest meter fills.

The project is small enough to understand quickly, but it already has a clear gameplay loop:

- Generate a road grid and city layout from a seed.
- Spawn the player, destination, and police once the city is ready.
- Let the car physics, AI pursuit, arrest logic, and win/lose UI handle the round.

## Stack

- Unity `2022.3.62f3`
- Cinemachine `2.10.5`
- AI Navigation `1.1.7`
- TextMesh Pro / UGUI

## Scenes

- `Assets/Scenes/Start.unity`: menu / entry point
- `Assets/Scenes/Instructions.unity`: instructions screen
- `Assets/Scenes/City.unity`: procedural chase gameplay
- `Assets/Scenes/Car Test.unity`: sandbox for vehicle tuning

## Controls

- `W / S` or vertical axis: throttle / reverse
- `A / D` or horizontal axis: steer
- `Space`: brake

## Project Layout

### Core gameplay scripts

- `Assets/Scripts/City/CityGen.cs`: generates the logical road graph
- `Assets/Scripts/City/CityBuilder.cs`: instantiates roads, buildings, floor, and navmesh
- `Assets/Scripts/PlayerSpawner.cs`: waits for city generation and spawns the player
- `Assets/Scripts/PoliceManager.cs`: spawns police cars onto road cells
- `Assets/Scripts/PoliceAI.cs`: pathfinding, pursuit steering, braking, and stuck recovery
- `Assets/Scripts/PoliceArrest.cs`: arrest meter and game-over trigger
- `Assets/Scripts/WinSystem.cs`: destination win detection
- `Assets/Scripts/GameOverManager.cs`: win/lose panels, restart, menu flow

### Vehicle scripts

- `Assets/Scripts/Car/CarController.cs`: steering, acceleration, braking, anti-roll, stability
- `Assets/Scripts/Car/WheelPhysics.cs`: suspension, grip, brake force, wheel visuals
- `Assets/Scripts/Car/PlayerDriver.cs`: player input bridge
- `Assets/Scripts/Car/CarVfxSfx.cs`: engine audio, skid audio, particles, and trails

### Supporting scripts

- `Assets/Scripts/DestinationMarker.cs`: spawns a destination marker once the city exists
- `Assets/Scripts/PoliceSiren.cs`: police siren audio setup
- `Assets/Scripts/MusicManager.cs`: persistent background music singleton
- `Assets/Scripts/MenuCarButton.cs`: menu vehicle selection / scene actions

## How the round works

1. `CityBuilder` reads `CitySettings` and generates a seeded grid.
2. Roads, buildings, the floor plane, and navmesh are built from that grid.
3. `PlayerSpawner` places the player at the generated spawn point.
4. `PoliceManager` places police vehicles onto valid road cells and assigns the player as target.
5. `WinSystem` watches for arrival at the destination while `PoliceArrest` tracks capture pressure.
6. `GameOverManager` pauses time and reveals the correct end screen.

## Tuning Guide

If you want to iterate quickly, these are the most useful files:

- City shape and density: `Assets/Scripts/City/CitySettings.cs`
- Road generation rules: `Assets/Scripts/City/CityGen.cs`
- Vehicle feel: `Assets/Scripts/Car/CarController.cs`
- Tire behavior and suspension: `Assets/Scripts/Car/WheelPhysics.cs`
- Police driving behavior: `Assets/Scripts/PoliceAI.cs`
- Arrest pressure and difficulty: `Assets/Scripts/PoliceArrest.cs`

## Running The Project

1. Open the project in Unity `2022.3.62f3`.
2. Open `Assets/Scenes/Start.unity` for the intended flow, or `Assets/Scenes/City.unity` for direct gameplay work.
3. Confirm the required prefabs and scene references are assigned in the inspector.
4. Press Play.

## Notes

- The city generation is deterministic when `randomSeed` is disabled.
- Police spawning depends on `CityBuilder.ready`, so runtime order matters.
- Cinemachine camera hookup happens in `PlayerSpawner`, not in the city generator.
- The scripts are structured so the procedural systems can be tuned without editing scene logic every pass.

## Recent Cleanup

The current script pass fixed a few obvious runtime hazards:

- corrected two Unity script/class name mismatches
- restored win detection after delayed player spawn
- reset menu click state when re-entering the menu
- added camera null safety during player spawn
- added a wheel-raycast fallback when expected drivable layers are missing
- avoided `DestroyImmediate` for runtime child cleanup

This is a good base for the next pass: balancing car feel, improving police behavior under heavy collisions, and tightening the city art pass.
