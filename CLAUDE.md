# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Context

**photon-kats** is a multiplayer kart racing game prototype built for a TwoPeaks studio tech test. It is a **kart racer + deckbuilder hybrid** (think Mario Kart meets a card game — cards replace on-track pickups). Networking is via **Photon Fusion 2** (state-authority/shared-mode deterministic networking). The base game framework comes from the Unity Karting Microgame tutorial.

## Unity & Environment

- **Unity version**: 6000.4.2f1 (Unity 6 LTS)
- **Render pipeline**: URP v17.4.0 — always use URP-compatible shaders/materials
- **Input system**: New Input System (`com.unity.inputsystem`) — never use legacy `Input.*` APIs
- **Networking**: Photon Fusion 2 (install manually — see below)
- **Version control**: Plastic SCM

## Folder Convention

All new custom code and assets must live under **`Assets/_MyAssets/`**, organized by domain:

```
Assets/_MyAssets/
├── Scripts/
│   ├── Networking/      # Fusion 2 NetworkBehaviours, spawning, RPCs
│   ├── Kart/            # Kart controller extensions, Fusion-driven movement
│   ├── Cards/           # Deckbuilder logic: deck, hand, card effects
│   ├── Race/            # Lap tracking, race state, leaderboard
│   └── UI/              # HUD, card hand display, race results
├── Prefabs/
├── Scenes/
├── Art/
└── Audio/
```

Do **not** add game code to `Assets/Karting/` — treat that folder as a read-only reference. The tutorial ML-Agents content (`Karting/AddOns/`, ML training scenes) is legacy and should not be extended.

## Code Standards

- **Comment all public members and non-obvious logic** — XML doc comments (`/// <summary>`) on public classes and methods, inline comments for complex blocks.
- **NetworkBehaviour over MonoBehaviour** for any object that crosses the network.
- Keep Fusion-specific code (`[Networked]`, `Runner`, `RPC_*`) isolated in `Networking/` — game logic classes should not directly reference Fusion types.

## Installing Photon Fusion 2

Fusion 2 is not in the Package Manager registry. Install it manually:

1. Download the Fusion 2 SDK from the Photon dashboard (photonengine.com)
2. In Unity: **Assets > Import Package > Custom Package** → select the downloaded `.unitypackage`
3. Add your **App ID** in `Fusion/Resources/PhotonAppSettings.asset`
4. The SDK will land in `Assets/Photon/` — do not move or modify files there

## Removed Packages (do not re-add)

These were in the original tutorial manifest and were removed — they are not needed for this project:

| Package | Reason removed |
|---|---|
| `com.unity.ml-agents` | Tutorial ML/AI training only |
| `com.unity.barracuda` | Neural net inference for ML-Agents only |
| `com.unity.learn.iet-framework` | Unity Learn tutorial UI |
| `com.unity.connect.share` | Asset sharing tool, not needed |
| `com.unity.multiplayer.center` | Multiplayer wizard UI, superseded by Fusion |
| `com.unity.visualscripting` | Not used; we write code |

## Key Existing Systems (from Karting template)

These live in `Assets/Karting/Scripts/` and are the starting point for kart mechanics:

- **`ArcadeKart.cs`** — core kart physics controller (acceleration, steering, grip). The Fusion-networked kart controller should extend or wrap this.
- **`GameFlowManager.cs`** — master game state machine (pre-race → race → results).
- **`ObjectiveCompleteLaps.cs`** / **`LapObject.cs`** — lap counting logic.
- **`KartSystems/Inputs/BaseInput.cs`** — input abstraction; implement a `FusionInput` version for networked input gathering.

## Building & Running

No build scripts — all development is through the **Unity Editor**:

- Open project in Unity 6000.4.2f1
- Main scene: `Assets/Karting/Scenes/` (pick the relevant race scene)
- Press **Play** to run locally; use Fusion's **Shared Mode** for multiplayer testing with multiple Editor instances

## Package Overview

| Package | Purpose |
|---|---|
| `com.unity.render-pipelines.universal` | URP rendering |
| `com.unity.inputsystem` | Player input |
| `com.unity.cinemachine` | Camera rigs |
| `com.unity.ai.navigation` | NavMesh for AI karts |
| `com.unity.probuilder` | In-editor track geometry |
| `com.unity.timeline` | Cutscenes / race intros |
| `com.unity.burst` + `com.unity.mathematics` | Performance math |
| `com.unity.test-framework` | Unit / integration tests |
