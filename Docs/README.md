# HeistNSeek Documentation

Welcome to the comprehensive documentation for the **HeistNSeek** project. This documentation is designed to help developers understand the architecture, systems, and patterns used throughout the codebase.

## Quick Navigation

### For First-Time Developers
1. Start with **[00_PROJECT_STRUCTURE.md](00_PROJECT_STRUCTURE.md)** - Understand folder organization
2. Read **[01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md)** - Learn core architectural patterns
3. Reference **[03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md)** - Common tasks and patterns

### For Specific Tasks
- **Adding a new feature?** → [03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#common-tasks)
- **Need to find a file?** → [04_FILE_REFERENCE.md](04_FILE_REFERENCE.md)
- **Understanding a system?** → [02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md)
- **Debugging an issue?** → [03_DEVELOPER_GUIDE.md](03_DEVELOPER_GUIDE.md#testing--debugging)

### For Deep Dives
- **Player System Details** → [02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md#1-player-system)
- **Inventory System Details** → [02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md#2-inventory-system)
- **Network System Details** → [02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md#3-network-system)
- **Event System Details** → [02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md#4-event-system)
- **State Machine Details** → [02_KEY_SYSTEMS.md](02_KEY_SYSTEMS.md#5-game-state-machine)

## Documentation Files

### 00_PROJECT_STRUCTURE.md
**What it contains:**
- High-level folder structure
- Scripts organization
- File categories and purposes
- Naming conventions
- Module dependencies

**When to read:**
- First introduction to the project
- Finding where new code should go
- Understanding project organization

### 01_ARCHITECTURE_OVERVIEW.md
**What it contains:**
- Core architectural pillars
- Dependency Injection (VContainer) explanation
- State Machine architecture
- Event-driven communication
- Scene loading & EntryPoints
- Networking integration
- Architectural decisions & trade-offs

**When to read:**
- Understanding how systems work together
- Learning design patterns
- Implementing new architecture components
- Grasping the "why" behind design decisions

### 02_KEY_SYSTEMS.md
**What it contains:**
- Player System (movement, input, ragdoll, push)
- Inventory System (items, stacking, dropping)
- Network System (spawning, scopes, syncing)
- Event System (publishing, subscribing, categories)
- State Machine (states, transitions, lifecycle)
- DI Scopes (global, scene, per-player)
- System interactions & data flow diagrams

**When to read:**
- Understanding a specific system
- Seeing how systems interact
- Implementing features within existing systems
- Debugging system-specific issues

### 03_DEVELOPER_GUIDE.md
**What it contains:**
- Common development tasks with code examples
- Design patterns and usage
- Testing & debugging tips
- Performance considerations
- Common mistakes & solutions
- Quick checklist for new features

**When to read:**
- Implementing a new feature
- Need code examples
- Stuck on how to do something
- Need best practices

### 04_FILE_REFERENCE.md
**What it contains:**
- Complete file listing by location
- File purposes and key methods
- Namespace hierarchy
- Asset organization
- File relationships
- Finding code by responsibility/type/feature

**When to read:**
- Looking for a specific file
- Understanding file organization
- Finding related code
- Cross-referencing systems

## Key Concepts Summary

### Dependency Injection (VContainer)
All services use constructor injection instead of singletons. Services are registered in LifetimeScopes.

```csharp
// Register in BootstrapLifetimeScope (global)
builder.Register<GameStateMachine>(Lifetime.Singleton);

// Use via constructor injection
public MyService(GameStateMachine stateMachine) { }
```

### State Machine
Game flow controlled by GameStateMachine with explicit state transitions.

```csharp
// Transition between states
StateMachine.Enter<GameplayState>();

// States publish events for subscribers
_messageHub.Subscribe<StateEnteredEvent>(OnStateEntered);
```

### Event-Driven Communication
Loosely-coupled systems communicate via MessageHub pub/sub.

```csharp
// Publish
_messageHub.Publish(new MyEvent { Data = 42 });

// Subscribe
_messageHub.Subscribe<MyEvent>(OnMyEvent);
```

### Scene Architecture
Bootstrap scene (root DI) → Main scene (additive load) with per-scope services.

```
Bootstrap.unity (BootstrapLifetimeScope)
    ↓ (Additive load)
Main.unity (GameplayLifetimeScope)
```

## Project Structure at a Glance

```
Assets/Scripts/
├── Core/                      # Core systems
│   ├── InjectionBase/         # VContainer LifetimeScopes
│   ├── StateMachine/          # Game state machine
│   ├── LevelInitializers/     # Scene EntryPoints
│   ├── Player/                # Player systems
│   ├── Inventory/             # Inventory systems
│   ├── Network/               # Network systems
│   └── EasyMessageHub/        # MessageHub integration
├── Events/                    # Event definitions
├── EditorScripts/             # Editor utilities
└── Helpers/                   # Shared utilities (DO NOT MODIFY)
```

## Common Development Workflows

### Adding a New Player Mechanic
1. Create component in `Core/Player/Mechanics/`
2. Create event(s) in `Events/`
3. Subscribe to events where needed
4. Publish events when mechanic activates
5. Test with Bootstrap → Main flow

### Adding Inventory Items
1. Create ItemDataSO in `Assets/SO/`
2. Create ItemPickup in `Core/Inventory/SessionInventory/`
3. Listen to ItemAddedEvent in UI/systems
4. Drop items on ragdoll trigger
5. Test stacking and weight

### Creating a Game State
1. Create state in `Core/StateMachine/States/`
2. Register in `BootstrapLifetimeScope.Configure()`
3. Implement Enter/Exit logic
4. Transition via `StateMachine.Enter<YourState>()`
5. Listen to state events if needed

### Adding Network Behavior
1. Extend `NetworkBehaviour`
2. Check `IsOwner` for owner-only logic
3. Use `[Rpc]` attributes for syncing
4. Register in `PlayerScope` for per-player services
5. Test with multiple clients

## Important Guidelines

### DO ✅
- Use constructor injection for all dependencies
- Create events for game events and state changes
- Subscribe to events using MessageHub
- Register services in appropriate LifetimeScopes
- Use [Inject] attribute for MonoBehaviour dependencies
- Check IsOwner in NetworkBehaviour
- Use SubscribeSafe for MonoBehaviour subscriptions
- Test from Bootstrap scene

### DON'T ❌
- Use GetComponent as service locator
- Create singletons manually
- Use new for managed services
- Modify Helpers/ folder
- Run scenes directly (always Bootstrap first)
- Leave subscriptions unmanaged in MonoBehaviours
- Process input in non-owner clients
- Ignore DI guidelines

## Getting Help

1. **Check the documentation** - These guides cover 90% of questions
2. **Look at existing code** - Similar patterns exist elsewhere
3. **Read CLAUDE.md** - Project-specific guidelines in repo root
4. **Use Debug.Log** - Trace execution and understand flow
5. **Ask in team channels** - Others may have solved it

## Project Information

- **Unity Version:** 6 (6000.2.10f1)
- **Main Architecture:** VContainer + State Machine + Event-Driven
- **Multiplayer:** Unity Netcode for GameObjects
- **Key Dependencies:**
  - VContainer 1.17.0 (Dependency Injection)
  - Easy.MessageHub 5.1.0 (Event Bus)
  - Unity Netcode (Multiplayer)
  - FPS Engine (from ThirdParty/Cowsins)

## Document Maintenance

These documents are maintained to reflect the current state of the codebase:
- Update when adding new systems
- Keep examples current
- Reflect actual implementation
- Add new patterns when discovered

Last Updated: 2025-12-13

---

**Happy coding!** 🚀

For questions about these documents or the architecture, refer to the team's documentation standards and the CLAUDE.md file in the project root.
