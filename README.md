# 3D Wakeboarding Video Game

A wakeboarding game built in Unity where you carve back and forth behind a boat, jump the wake, and perform different tricks.

## About the Game

You ride behind a boat that cruises forward at a constant speed, connected by a rope. Steer side to side, and the harder you cut into the wake, the higher you jump. While airborne you can do different spins
and flips. You cannot fall in this game, every jump is landed.

The entire scene is generated procedurally in code at startup (GameBootstrap.cs), rather than being hand-placed in the Unity editor.

## Controls

| Key(s) | Action                                                         |
| --- |----------------------------------------------------------------|
| `A` / `D` | Carve left / right (on water)                                  |
| `Space` | Bunny hop                                                      |
| `Left Arrow` / `Right Arrow` | Spin (air: 360/540/720, water: quick 180)                      |
| `Up Arrow` | Frontflip                                                      |
| `Down Arrow` | Backflip                                                       |
| `Up Arrow` + `Down Arrow` (held together) | Raley (heelside jumps only, requires a committed carve into the wake) |

## Getting Started

### Prerequisites

- [Unity](https://unity.com/download) 2022.3.50f1 (or a compatible 2022.3 LTS release)

### Installation

1. Clone the repository:
   ```
   git clone <repo-url>
   ```
2. Open Unity Hub and add the `WakeGame` folder as a project.
3. Open the project, then open the `WakeboardScene` scene (`Assets/Scenes/WakeboardScene.unity`).
4. Press Play in the Unity editor.

## Project Structure

```
WakeGame/
├── Assets/
│   ├── Scenes/          # Main scene (WakeboardScene)
│   └── Scripts/         # Gameplay, physics, and rendering scripts
├── ProjectSettings/      # Unity project configuration
└── Packages/             # Unity package dependencies
```

Key scripts:

- `GameBootstrap.cs` — procedurally builds the entire scene (water, land, boat, rider, camera, lighting) on startup
- `RiderController.cs` — rider physics: carving, wake jumps, spins, flips, and the raley trick
- `BoatMover.cs` — constant-speed boat movement
- `RopeRenderer.cs` / `WakeRenderer.cs` / `SplashEffect.cs` — visual effects for the tow rope, wake, and water spray
- `CameraFollow.cs` — third-person camera that trails the boat

## Author

Evan Busby