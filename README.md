# Runaway

Runaway is a compact arcade driving prototype built in Unity. You spawn into a procedurally generated city, thread through tight road networks, avoid police pressure, and reach the destination before the arrest meter fills.

The project is small enough to understand quickly, but it already has a clear gameplay loop:

- Generate a road grid and city layout from a seed.
- Spawn the player, destination, and police once the city is ready.
- Let the car physics, AI pursuit, arrest logic, and win/lose UI handle the round.

## Project Highlights

- Graph-based procedural streets: `CityGen` synthesizes a 4-neighbor road graph using arterial spacing, Bernoulli-style local street sampling, random loop injection, probabilistic edge removal, and iterative dead-end pruning.
- Connectivity is enforced algorithmically: spawn and destination validity are protected with breadth-first reachability checks, component pruning, reciprocal edge cleanup, and a fallback connector carve so the generated graph remains traversable.
- Vehicle dynamics are custom: `WheelPhysics` uses per-wheel raycast suspension, spring-damper force calculation, contact-patch lateral slip estimation, friction-circle style force clamping, and rolling resistance; `CarController` layers Ackermann steering geometry, anti-roll bars, downforce, and yaw-rate damping on top.
- Police driving uses path-following instead of homing: `PoliceAI` computes NavMesh paths with `NavMesh.CalculatePath`, then applies lookahead pursuit, signed-angle steering control, exponential steering smoothing, lane-offset biasing, corner-speed scheduling, and timed reverse-based stuck recovery.
- Failure pressure is modeled as a continuous meter: `PoliceArrest` uses non-alloc overlap queries, nearest-threat evaluation, proximity-based fill rates, surround-state detection, and contact-based bonus accumulation rather than a binary collision trigger.
- The city build pipeline is staged: `CityBuilder` generates the logical grid, instantiates geometry, fits large building footprints to cell rectangles, rebuilds the NavMesh, and only then releases dependent systems through `CityBuilder.ready`.
- Tuning is parameterized at the system level: road topology, spawn heuristics, suspension stiffness, grip coefficients, AI braking thresholds, and arrest timings are exposed as serialized values, which makes the prototype easy to iterate as a systems-driven game rather than a hardcoded demo.

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
- `Assets/Scripts/City/CityBuilder.cs`: instantiates roads, buildings, floor, and NavMesh
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
2. Roads, buildings, the floor plane, and NavMesh are built from that grid.
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