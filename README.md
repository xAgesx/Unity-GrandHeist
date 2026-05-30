# Grand Heist 
Check The Game Live At : https://xagesx.itch.io/grand-heist
## WATCH HERE ( click the image )
[![Watch the video](https://img.youtube.com/vi/k_dNe8nFu4g/maxresdefault.jpg)](https://youtu.be/k_dNe8nFu4g)

*A stealth heist game built in Unity.*

---

## Overview

Grand Heist is a first-person stealth game where you infiltrate a heavily guarded bank. Navigate patrol routes, avoid security cameras and laser tripwires, collect keycards to unlock restricted areas, and pull off the perfect heist — without getting caught.

## Features

- **Guard AI** — Guards patrol waypoints, investigate noises, search last-known positions, and chase the player on sight. Powered by a custom behaviour-tree system (Selector, Sequence, Condition, Action nodes).
- **Security Cameras** — Sweeping cameras that lock onto the player and trigger a global alarm when detected.
- **Laser Tripwires** — Sweeping, pulsing laser beams with proximity-based audio, particle-spark effects, and instant game-over on prolonged contact.
- **Stamina & Movement** — Walk, run (with stamina drain), and crouch (reduces guard sight range). Each movement type produces different noise levels that nearby guards can hear.
- **Keycard System** — Green, Blue, and Red keycards required to open matching locked doors.
- **Sound Indicators** — Real-time HUD showing how much noise you're making and a detection meter that fills when spotted.
- **Pause / Game Over / Restart** — Full pause menu with music/SFX toggles, game-over screen with restart option.

## Controls

| Key              | Action                |
| ---------------- | --------------------- |
| `W` `A` `S` `D`  | Move                  |
| `Left Shift`     | Sprint (uses stamina) |
| `Left Control` / `C` | Crouch           |
| `E`              | Interact (pick up keycards, open doors) |
| `R`              | Restart level         |
| `Escape`         | Pause / Resume        |

## Gameplay

1. **Stay hidden** — Crouch to reduce your visibility and noise footprint.
2. **Manage stamina** — Sprinting depletes stamina; regenerate by walking or standing still.
3. **Collect keycards** — Find Green, Blue, and Red keycards scattered throughout the level.
4. **Unlock doors** — Use matching keycards to progress deeper into the bank.
5. **Avoid detection** — Guards, cameras, and lasers will trigger an alarm. If your detection meter fills up, it's game over.

## Built With

- **Unity** (URP)
- **C#** — Custom behaviour-tree implementation for guard AI
- **ProBuilder** — Level-blockout tools
- **Polygon Heist** — Low-poly asset pack
- **Dark Geo GUI Kit** — UI styling

## Getting Started

Open the project in Unity (URP template) and load the `Game` scene.

```
Assets/Game.unity
```

Build settings are already configured — build and run for a standalone Windows executable.
