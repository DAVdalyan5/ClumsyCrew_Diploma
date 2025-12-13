# HeistNSeek Project Structure

**Project Type:** Unity 6 (6000.2.10f1) Multiplayer Game
**Architecture:** VContainer DI + State Machine + Netcode
**Last Updated:** 2025-12-13

## High-Level Organization

```
HeistNSeek/
├── Assets/
│   ├── Scripts/              # All game logic (C#)
│   ├── Scenes/              # Unity scene files
│   ├── Prefabs/             # Reusable prefab assets
│   ├── SO/                  # ScriptableObject definitions
│   ├── Materials/           # Material assets
│   ├── Animations/          # Animation clips & controllers
│   ├── ThirdParty/          # External packages (FPS Engine, etc.)
│   ├── Plugins/             # Plugin packages (UniTask, etc.)
│   ├── Editor/              # Editor-only resources
│   ├── Configs/             # Project configuration files
│   └── Settings/            # Project settings
├── Docs/                    # Developer documentation
├── Builds/                  # Build outputs
└── ProjectSettings/         # Unity project configuration
```

## Scripts Folder Structure

Located at `Assets/Scripts/`

### Core Systems

```
Core/
├── InjectionBase/           # VContainer LifetimeScope configurations
│   ├── BootstrapLifetimeScope.cs         # Root DI scope (persists across scenes)
│   └── GameplayLifetimeScope.cs          # Scene-specific DI scope
│
├── StateMachine/            # Game state machine & states
│   ├── GameStateMachine.cs               # Central state machine
│   ├── IState.cs                         # State interface
│   ├── BaseState.cs                      # Base state implementation
│   ├── StateMachineExample.cs            # Usage example
│   └── States/
│       ├── BootstrapState.cs             # Bootstrap initialization
│       ├── LoadingState.cs               # Scene loading
│       ├── GameplayState.cs              # Active gameplay
│       └── MenuState.cs                  # Menu state (example)
│
├── LevelInitializers/       # Scene entry points (run after DI setup)
│   ├── BootstrapEntryPoint.cs            # Bootstrap scene init
│   └── GameplayEntryPoint.cs             # Main scene init
│
├── EasyMessageHub/          # MessageHub integration (pub/sub)
│   ├── MessageHubExtensions.cs           # MessageHub helpers
│   ├── DisposeMethod.cs                  # Disposal patterns
│   └── MessagingExample.cs               # Usage examples
│
└── DI/                      # DI utilities (reserved for future use)
```

### Game Systems

```
Player/                      # Player-related systems
├── PlayerController.cs                   # Main player controller (NetworkBehaviour)
├── FirstPersonInputService.cs            # Input handling service
├── FirstPersonMovementHandler.cs         # Movement logic
├── Character/
│   ├── CharacterAnimationController.cs   # Animation control
│   └── Models/                           # Character data models (reserved)
├── Mechanics/
│   ├── Pushing/
│   │   ├── CharacterPusher.cs            # Push mechanic implementation
│   │   └── IPushable.cs                  # Push interface
│   └── Ragdoll/
│       ├── CharacterBalancer.cs          # Character balance system
│       ├── BalanceInfo.cs                # Balance state data
│       ├── CollisionDetector.cs          # Collision detection
│       └── RagdollUtilities.cs           # Ragdoll utilities
├── Animation/
│   └── OwnerAuthoritativeNetworkAnimator.cs  # Network animation sync
└── PlayerMarkers/
    ├── CameraRootMarker.cs               # Camera position marker
    └── PushPositionMarker.cs             # Push mechanic position marker
```

```
Inventory/                   # Inventory & item systems
├── Enums.cs                              # Inventory enumerations
├── Models/
│   ├── ItemDataSO.cs                     # Item data (ScriptableObject)
│   ├── WeaponDataSO.cs                   # Weapon data
│   ├── WeaponModel.cs                    # Weapon runtime model
│   └── ScatterConfigSO.cs                # Item scatter configuration
├── GlobalInventory/
│   └── anotherTest.cs                    # Global inventory test
└── SessionInventory/
    ├── SessionInventory.cs               # Session-specific inventory manager
    ├── InventoryItem.cs                  # Individual inventory item
    ├── ItemPickup.cs                     # Item pickup logic
    └── ItemDropper.cs                    # Item dropping logic
```

```
Network/                     # Multiplayer & networking
├── PlayerSpawner.cs                      # Player spawning on network join
├── PlayerScope.cs                        # Per-player DI scope
└── ClientSidePlayerConfigurator.cs       # Client-side player setup
```

### Utilities & Infrastructure

```
EditorScripts/              # Unity Editor utilities
├── SceneDropdownAttribute.cs             # Custom [SceneDropdown] attribute
└── SceneDropdownDrawer.cs                # Property drawer for scenes

Events/                     # Event definitions for MessageHub
├── Actions/
│   ├── InteractEvent.cs
│   ├── JumpEvent.cs
│   ├── PushEvent.cs
│   └── ResetBalanceEvent.cs
├── BalanceLostEvent.cs                   # Character lost balance
├── BalanceRegainedEvent.cs               # Character regained balance
├── ItemAddedEvent.cs                     # Item added to inventory
├── ItemRemovedEvent.cs                   # Item removed from inventory
├── ItemsDroppedEvent.cs                  # Items dropped from inventory
├── StateEnteredEvent.cs                  # State machine state entered
└── StateExitedEvent.cs                   # State machine state exited

Helpers/                    # Reusable utility classes (DO NOT MODIFY)
├── CoroutineHelper.cs                    # Run coroutines without MonoBehaviour
├── SceneLoader.cs                        # Async scene loading
├── FpsCounter.cs                         # FPS display utility
├── Extensions.cs                         # General extension methods
├── EnumerableExtensions.cs               # Collection utilities
└── VariableLister.cs                     # Variable listing utility
```

## Scenes Folder Structure

Located at `Assets/Scenes/`

```
Bootstrap.unity             # Entry point scene
├── BootstrapLifetimeScope (DontDestroyOnLoad)
├── BootstrapEntryPoint
└── Global services initialization

Main.unity                  # Gameplay scene (loaded additively)
├── GameplayLifetimeScope
├── GameplayEntryPoint
├── Player spawning
└── Gameplay-specific services
```

## Key File Categories

| Category | Location | Purpose |
|----------|----------|---------|
| **DI Setup** | `Core/InjectionBase/` | Service registration & scope configuration |
| **State Management** | `Core/StateMachine/` | Game flow & phase transitions |
| **Scene Init** | `Core/LevelInitializers/` | EntryPoint classes for scenes |
| **Events** | `Events/` | Event definitions for pub/sub messaging |
| **Player Logic** | `Core/Player/` | Player controller, movement, mechanics |
| **Inventory** | `Core/Inventory/` | Item & weapon systems |
| **Networking** | `Core/Network/` | Multiplayer & player spawning |
| **Utilities** | `Helpers/` & `EditorScripts/` | Reusable tools & editor extensions |

## Naming Conventions

- **Namespaces:** `HeistNSeek.Core`, `HeistNSeek.Core.Player`, `HeistNSeek.Events`, etc.
- **Classes:** PascalCase (e.g., `PlayerController`, `SessionInventory`)
- **Interfaces:** PascalCase with `I` prefix (e.g., `IState`, `IPushable`)
- **Private fields:** `_camelCase` (e.g., `_messageHub`, `_items`)
- **Constants:** `UPPER_SNAKE_CASE`
- **ScriptableObjects:** End with `SO` (e.g., `ItemDataSO`, `WeaponDataSO`)

## File Guidelines

- **One class per file** (except small supporting classes)
- **Max 200 lines per file** (aim for this, not absolute)
- **Place in appropriate folder** matching responsibility
- **New code under `Assets/Scripts/`** only
- **DO NOT modify** `Helpers/` folder (unless instructed)
- **Scene references** use `[SceneDropdown]` attribute

## Module Dependencies

```
Bootstrap Scene
    ↓
BootstrapLifetimeScope
    ├─→ MessageHub (singleton)
    ├─→ GameStateMachine (singleton)
    ├─→ All Game States (singleton)
    └─→ BootstrapEntryPoint
            ↓
        [Game Loop Starts]
            ↓
        Main Scene (Additive Load)
            ↓
        GameplayLifetimeScope
            ├─→ Player Systems
            ├─→ Inventory Systems
            ├─→ Network Manager
            └─→ GameplayEntryPoint
```

## Important Notes

- **DontDestroyOnLoad:** Only BootstrapLifetimeScope persists across scenes
- **State Machine:** Controls game flow; transitions trigger events via MessageHub
- **Networking:** Uses Unity Netcode; PlayerSpawner handles multiplayer spawning
- **EntryPoints:** Run after all DI setup; use for initialization logic
- **MessageHub:** Global pub/sub system for loosely-coupled communication
