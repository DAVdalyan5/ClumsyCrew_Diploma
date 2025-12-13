# Complete File Reference

## Core Systems

### Dependency Injection Base
**Location:** `Assets/Scripts/Core/InjectionBase/`

| File | Purpose | Key Methods |
|------|---------|-------------|
| **BootstrapLifetimeScope.cs** | Root DI scope (persists across scenes) | `Configure(IContainerBuilder)` - Register global services |
| **GameplayLifetimeScope.cs** | Scene-specific DI scope | `Configure(IContainerBuilder)` - Register gameplay services |

**Key Dependencies Registered:**
```
BootstrapLifetimeScope
├── IMessageHub (singleton)
├── GameStateMachine (singleton)
├── BootstrapState (singleton)
├── LoadingState (singleton)
├── GameplayState (singleton)
└── BootstrapEntryPoint (entry)

GameplayLifetimeScope
├── SessionInventory (singleton)
├── GameplayEntryPoint (entry)
└── [Scene-specific services]
```

---

### State Machine
**Location:** `Assets/Scripts/Core/StateMachine/`

| File | Purpose | Key Methods/Properties |
|------|---------|------------------------|
| **GameStateMachine.cs** | Central state machine | `Enter<TState>()`, `ActiveState` |
| **IState.cs** | State interface | `Enter(payload)`, `Exit()`, `SetStateMachine()` |
| **BaseState.cs** | Base state implementation | `StateMachine` protected property |
| **StateMachineExample.cs** | Usage example | (Example code only) |

#### State Classes
**Location:** `Assets/Scripts/Core/StateMachine/States/`

| File | Lifecycle | Transitions | Notes |
|------|-----------|-------------|-------|
| **BootstrapState.cs** | Game start | → LoadingState | Loads Main scene additively |
| **LoadingState.cs** | Scene loading | → GameplayState | Async scene loading |
| **GameplayState.cs** | Active gameplay | → LoadingState | Resume/resume game |
| **MenuState.cs** | Menu (example) | → GameplayState | Example state, not registered |

**State Flow:**
```
BootstrapState
    ↓ (loads Main.unity)
LoadingState
    ↓ (completes load)
GameplayState
```

---

### Level Initialization
**Location:** `Assets/Scripts/Core/LevelInitializers/`

| File | Purpose | Triggered By |
|------|---------|--------------|
| **BootstrapEntryPoint.cs** | Bootstrap initialization | `BootstrapLifetimeScope` (IStartable) |
| **GameplayEntryPoint.cs** | Gameplay initialization | `GameplayLifetimeScope` (IStartable) |

**Execution Order:**
```
1. Scene loads
2. LifetimeScope.Configure() runs
3. VContainer resolves dependencies
4. EntryPoint.Start() runs (IStartable)
5. Your logic executes
```

---

### MessageHub Integration
**Location:** `Assets/Scripts/Core/EasyMessageHub/`

| File | Purpose | Key Features |
|------|---------|--------------|
| **MessageHubExtensions.cs** | MessageHub helper methods | `SubscribeSafe<T>()` - Auto-unsubscribe |
| **DisposeMethod.cs** | Disposal patterns | Subscription management |
| **MessagingExample.cs** | Usage examples | Reference patterns |

**Key Extension:**
```csharp
// Auto-unsubscribes when 'target' is destroyed
messageHub.SubscribeSafe<MyEvent>(target, handler);
```

---

## Player Systems

### Main Controller
**Location:** `Assets/Scripts/Core/Player/`

| File | Responsibility | Key Methods |
|------|-----------------|-------------|
| **PlayerController.cs** | Main player controller (NetworkBehaviour) | `EnablePlayerRagdoll()`, `DisablePlayerRagdoll()`, `TriggerPushRagdoll()` |
| **FirstPersonInputService.cs** | Input capture | `GetMovementInput()`, `GetJumpInput()` |
| **FirstPersonMovementHandler.cs** | Movement logic (FPS Engine) | `Move()`, `Jump()` |

### Character System
**Location:** `Assets/Scripts/Core/Player/Character/`

| File | Purpose | Key Methods |
|------|---------|-------------|
| **CharacterAnimationController.cs** | Animation control | `PlayAnimation()`, `StopAnimation()` |

**Models Subfolder:** `Core/Player/Character/Models/`
- Currently empty, reserved for character data models

### Mechanics - Pushing
**Location:** `Assets/Scripts/Core/Player/Mechanics/Pushing/`

| File | Purpose | Key Methods |
|------|---------|-------------|
| **CharacterPusher.cs** | Push mechanic | `Push(IPushable target)` |
| **IPushable.cs** | Interface for pushable objects | `ReceivePush(Vector3 direction, float force)` |

### Mechanics - Ragdoll & Balance
**Location:** `Assets/Scripts/Core/Player/Mechanics/Ragdoll/`

| File | Purpose | Key Methods/Properties |
|------|---------|------------------------|
| **CharacterBalancer.cs** | Balance management | Monitors balance state |
| **BalanceInfo.cs** | Balance state data | `IsBalanced` property |
| **CollisionDetector.cs** | Collision detection | `Subscribe()`, triggers on impact |
| **RagdollUtilities.cs** | Ragdoll utilities | `ToggleRagdoll()`, `IsRagdollEnabled()` |

### Player Markers
**Location:** `Assets/Scripts/Core/Player/PlayerMarkers/`

| File | Purpose | Key Methods |
|------|---------|-------------|
| **CameraRootMarker.cs** | Camera position marker | Marks camera location |
| **PushPositionMarker.cs** | Push mechanic marker | Marks push origin |

### Animation & Network
**Location:** `Assets/Scripts/Core/Player/Animation/`

| File | Purpose | Key Methods |
|------|---------|-------------|
| **OwnerAuthoritativeNetworkAnimator.cs** | Network animation sync | `SetAnimatorParam()`, broadcasts to network |

---

## Inventory Systems

### Core Inventory
**Location:** `Assets/Scripts/Core/Inventory/SessionInventory/`

| File | Purpose | Key Methods |
|------|---------|-------------|
| **SessionInventory.cs** | Core inventory manager | `AddItem()`, `RemoveItem()`, `DropItems()`, `GetItemCount()` |
| **InventoryItem.cs** | Individual inventory entry | `CanStackWith()`, `AddToStack()`, `RemoveFromStack()` |
| **ItemPickup.cs** | Item pickup logic | `OnTriggerEnter()` |
| **ItemDropper.cs** | Item dropping logic | Spawns dropped items |

### Inventory Models
**Location:** `Assets/Scripts/Core/Inventory/Models/`

| File | Purpose | Key Properties |
|------|---------|-----------------|
| **ItemDataSO.cs** | Item definition (ScriptableObject) | `itemName`, `weight`, `isStackable`, `maxStackSize` |
| **WeaponDataSO.cs** | Weapon definition | Extends ItemDataSO with weapon stats |
| **WeaponModel.cs** | Weapon runtime model | Runtime weapon data |
| **ScatterConfigSO.cs** | Item scatter configuration | Drop spread configuration |

### Global Inventory
**Location:** `Assets/Scripts/Core/Inventory/GlobalInventory/`

| File | Purpose | Notes |
|------|---------|-------|
| **anotherTest.cs** | Global inventory test | Development/testing only |

### Inventory Enums
**Location:** `Assets/Scripts/Core/Inventory/`

| File | Purpose | Contains |
|------|---------|----------|
| **Enums.cs** | Inventory-related enums | Item types, rarity, etc. |

---

## Network Systems

### Network Management
**Location:** `Assets/Scripts/Core/Network/`

| File | Purpose | Key Methods |
|------|---------|-------------|
| **PlayerSpawner.cs** | Network player spawning | `SpawnPlayerForClient()`, `OnClientConnected()` |
| **PlayerScope.cs** | Per-player DI scope | Creates isolated service scope |
| **ClientSidePlayerConfigurator.cs** | Client player setup | `OnNetworkSpawn()` |

**Network Flow:**
```
Client connects
    ↓
OnClientConnected(clientId)
    ↓
PlayerSpawner.SpawnPlayerForClient()
    ↓
Create player with PlayerScope
    ↓
Client-side DI setup
    ↓
Player ready
```

---

## Event System

### Event Definitions
**Location:** `Assets/Scripts/Events/`

#### State Machine Events
| Event | Property | Fired When |
|-------|----------|------------|
| **StateEnteredEvent.cs** | `StateType`, `Payload` | State machine enters state |
| **StateExitedEvent.cs** | `StateType` | State machine exits state |

#### Player Action Events
**Location:** `Assets/Scripts/Events/Actions/`

| Event | Property | Fired When |
|-------|----------|------------|
| **InteractEvent.cs** | `TargetCollider` | Player interacts |
| **JumpEvent.cs** | Varies | Player jumps |
| **PushEvent.cs** | `PushDirection`, `PushForce` | Player pushes |
| **ResetBalanceEvent.cs** | - | Reset balance request |

#### Balance Events
| Event | Property | Fired When |
|-------|----------|------------|
| **BalanceLostEvent.cs** | `ImpactSpeed` | Player loses balance (collision) |
| **BalanceRegainedEvent.cs** | - | Player regains balance |

#### Inventory Events
| Event | Property | Fired When |
|-------|----------|------------|
| **ItemAddedEvent.cs** | `ItemData`, `Amount`, `TotalCount` | Item added to inventory |
| **ItemRemovedEvent.cs** | `ItemData`, `Amount`, `RemainingCount` | Item removed from inventory |
| **ItemsDroppedEvent.cs** | `DroppedItems`, `DropPosition`, `ImpactSpeed` | Items dropped |

---

## Editor Utilities

### Editor Scripts
**Location:** `Assets/Scripts/EditorScripts/`

| File | Purpose | Attribute |
|------|---------|-----------|
| **SceneDropdownAttribute.cs** | Custom dropdown for scene selection | `[SceneDropdown]` |
| **SceneDropdownDrawer.cs** | Property drawer for scene dropdown | Inspector rendering |

**Usage:**
```csharp
[SceneDropdown]
[SerializeField] private int targetSceneIndex;
```

---

## Helper Utilities
**Location:** `Assets/Scripts/Helpers/`

**⚠️ WARNING: DO NOT MODIFY** - These are shared utilities

| File | Purpose | Key Methods |
|------|---------|-------------|
| **CoroutineHelper.cs** | Run coroutines without MonoBehaviour | `RunCoroutine()` |
| **SceneLoader.cs** | Async scene loading | `Load()` |
| **FpsCounter.cs** | FPS display utility | Attach to GameObject |
| **Extensions.cs** | General extensions | String, int, bool helpers |
| **EnumerableExtensions.cs** | Collection extensions | `SafeAdd()`, `Shuffle()` |
| **VariableLister.cs** | Variable listing | Debugging utility |

---

## Asset Organization

### Scenes
**Location:** `Assets/Scenes/`

| Scene | Type | Entry Point | Notes |
|-------|------|-----------|-------|
| **Bootstrap.unity** | Bootstrap | BootstrapLifetimeScope | Must be first in build |
| **Main.unity** | Gameplay | GameplayLifetimeScope | Loaded additively |

### ScriptableObjects
**Location:** `Assets/SO/`

- **Weapons.meta** - Weapon configurations
- Item definitions (ItemDataSO instances)
- Weapon definitions (WeaponDataSO instances)

### Prefabs
**Location:** `Assets/Prefabs/`

| Prefab | Type | Purpose |
|--------|------|---------|
| **Enemies.meta** | Folder | Enemy prefabs |
| **MainPlayer.prefab** | Player | Main player prefab |
| **PlayerCapsule.prefab** | Player | Capsule-shaped player variant |
| **PlayerSpawner.prefab** | Network | Spawner for network players |

### Third-Party Assets
**Location:** `Assets/ThirdParty/`

| Package | Type | Version |
|---------|------|---------|
| **Cowsins/** | FPS Engine | Included |
| **UniTask/** | Async utility | Latest |

### Materials & Animations
**Location:** `Assets/ThirdParty/Cowsins/`

- **Materials/** - Material definitions
- **Animations/** - Animation clips & controllers

---

## Code File Statistics

### Core Files by Category

```
Dependency Injection:        2 files
State Machine:              6 files
Player Systems:            13 files
Inventory Systems:         10 files
Network Systems:            3 files
Events:                     12 files
Utilities:                  11 files
Editor Tools:               2 files
───────────────────────────
Total Core Files:           59 files
```

### File Size Guidelines

- **Small** (< 200 lines): Services, events, models
- **Medium** (200-500 lines): Controllers, managers
- **Large** (500+ lines): Complex systems (split if possible)

### Namespace Hierarchy

```
HeistNSeek
├── Core
│   ├── StateMachine
│   ├── Player
│   │   ├── Character
│   │   ├── Mechanics
│   │   │   ├── Pushing
│   │   │   └── Ragdoll
│   │   └── Animation
│   ├── Network
│   ├── Inventory
│   │   ├── SessionInventory
│   │   └── Models
│   └── EasyMessageHub
├── Events
│   └── Actions
└── Helpers
```

---

## Important File Relationships

### Initialization Chain
```
Bootstrap.unity
    ↓
BootstrapLifetimeScope.Configure()
    ├─→ Register GameStateMachine
    ├─→ Register States
    ├─→ Register EntryPoints
    └─→ Register global services
    ↓
BootstrapEntryPoint.Start()
    ├─→ StateMachine.Enter<BootstrapState>()
    │   ├─→ Load Main.unity (additive)
    │   └─→ StateMachine.Enter<LoadingState>()
    │       └─→ StateMachine.Enter<GameplayState>()
    │           ├─→ GameplayLifetimeScope.Configure()
    │           └─→ GameplayEntryPoint.Start()
    └─→ Game ready
```

### Event Flow
```
System publishes event
    ↓
IMessageHub.Publish<T>()
    ↓
All subscribers notified
    ├─→ UI updates
    ├─→ Audio plays
    ├─→ Effects spawn
    └─→ State changes
```

### Network Flow
```
Client connects
    ↓
PlayerSpawner.OnClientConnected()
    ↓
Spawn player NetworkObject
    ↓
PlayerScope per-player DI
    ↓
ClientSidePlayerConfigurator.OnNetworkSpawn()
    ↓
Inject services into PlayerController
    ↓
Player ready to play
```

---

## Finding Code

### By Responsibility
- **Input handling:** `FirstPersonInputService.cs`
- **Movement:** `FirstPersonMovementHandler.cs`
- **Ragdoll:** `RagdollUtilities.cs`, `CollisionDetector.cs`
- **Inventory:** `SessionInventory.cs`
- **Events:** `Events/` folder
- **Network:** `Core/Network/` folder
- **State management:** `Core/StateMachine/`

### By Type
- **Services:** `Core/` subdirectories
- **MonoBehaviours:** Located in `Core/Player/`, `Core/Network/`
- **Events:** `Events/` folder
- **Data:** `Models/` subfolders
- **Utilities:** `Helpers/` folder

### By Feature
- **Player feature:** `Core/Player/` + related events
- **Inventory feature:** `Core/Inventory/` + related events
- **Network feature:** `Core/Network/` + `Core/Player/Animation/`
- **Ragdoll feature:** `Core/Player/Mechanics/Ragdoll/` + events

