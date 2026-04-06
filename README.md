# Runaway

Runaway is a compact arcade driving prototype built in Unity. You spawn into a procedurally generated city, thread through tight road networks, avoid police pressure, and reach the destination before the arrest meter fills.

---

<img width="1920" height="1243" alt="Screenshot 2026-04-06 at 4 15 15 PM" src="https://github.com/user-attachments/assets/936c9cef-a3b7-4c5f-ad27-d9211c41771f" />

<img width="1395" height="940" alt="Screenshot 2026-04-06 at 4 18 11 PM" src="https://github.com/user-attachments/assets/65393564-0f24-4f49-849f-90b2219dfbee" />

<img width="1227" height="907" alt="Screenshot 2026-04-06 at 4 20 10 PM" src="https://github.com/user-attachments/assets/495ca000-d47e-454a-b9a6-adf74e02361b" />

---

The project is small enough to understand quickly, but it already has a clear gameplay loop:

- Generate a road grid and city layout from a seed.
- Spawn the player, destination, and police once the city is ready.
- Let the car physics, AI pursuit, arrest logic, and win/lose UI handle the round.

---

## Project Highlights

- Graph-based procedural streets: `CityGen` synthesizes a 4-neighbor road graph using arterial spacing, Bernoulli-style local street sampling, random loop injection, probabilistic edge removal, and iterative dead-end pruning.
- Connectivity is enforced algorithmically: spawn and destination validity are protected with breadth-first reachability checks, component pruning, reciprocal edge cleanup, and a fallback connector carve so the generated graph remains traversable.
- Vehicle dynamics are custom: `WheelPhysics` uses per-wheel raycast suspension, spring-damper force calculation, contact-patch lateral slip estimation, friction-circle style force clamping, and rolling resistance; `CarController` layers Ackermann steering geometry, anti-roll bars, downforce, and yaw-rate damping on top.
- Police driving uses path-following: `PoliceAI` computes NavMesh paths with `NavMesh.CalculatePath`, then applies lookahead pursuit, signed-angle steering control, exponential steering smoothing, lane-offset biasing, corner-speed scheduling, and timed reverse-based stuck recovery.
- Failure pressure is modeled as a continuous meter: `PoliceArrest` uses non-alloc overlap queries, nearest-threat evaluation, proximity-based fill rates, surround-state detection, and contact-based bonus accumulation rather than a binary collision trigger.
- The city build pipeline is staged: `CityBuilder` generates the logical grid, instantiates geometry, fits large building footprints to cell rectangles, rebuilds the NavMesh, and only then releases dependent systems through `CityBuilder.ready`.
- Tuning is parameterized at the system level: road topology, spawn heuristics, suspension stiffness, grip coefficients, AI braking thresholds, and arrest timings are exposed as serialized values, which makes the prototype easy to iterate as a systems-driven game rather than a hardcoded demo.

## Controls

- `W / S` or vertical axis: throttle / reverse
- `A / D` or horizontal axis: steer
- `Space`: brake
