# Architecture Overview

**Key Architectural Pattern:** Dependency Injection + State Machine + Event-Driven

## Core Pillars

### 1. Dependency Injection (VContainer)

The project uses **VContainer** (v1.17.0) for all dependency management. This removes the need for singletons and improves testability.

#### Two-Scope Architecture

```
┌─────────────────────────────────────────────┐
│        BootstrapLifetimeScope               │
│        (Root - DontDestroyOnLoad)           │
├─────────────────────────────────────────────┤
│ Global Services (live for entire game):     │
│  • IMessageHub (Easy.MessageHub)            │
│  • GameStateMachine                         │
│  • All Game States                          │
│  • BootstrapEntryPoint                      │
└─────────────────────────────────────────────┘
              ↓ (Loads additively)
┌─────────────────────────────────────────────┐
│       GameplayLifetimeScope                 │
│       (Scene-specific)                      │
├─────────────────────────────────────────────┤
│ Scene Services (destroyed on scene unload): │
│  • PlayerController                         │
│  • SessionInventory                         │
│  • Network managers                         │
│  • GameplayEntryPoint                       │
└─────────────────────────────────────────────┘
```

#### Registration Pattern

**BootstrapLifetimeScope** (`Assets/Scripts/Core/InjectionBase/BootstrapLifetimeScope.cs`)
```csharp
protected override void Configure(IContainerBuilder builder)
{
    // Register as Singleton (live forever)
    builder.RegisterInstance<IMessageHub>(messageHub);
    builder.Register<GameStateMachine>(Lifetime.Singleton);
    builder.Register<BootstrapState>(Lifetime.Singleton).As<IState>();

    // Register EntryPoint (runs after all DI setup)
    builder.RegisterEntryPoint<BootstrapEntryPoint>(Lifetime.Singleton);
}
```

**GameplayLifetimeScope** (`Assets/Scripts/Core/InjectionBase/GameplayLifetimeScope.cs`)
```csharp
protected override void Configure(IContainerBuilder builder)
{
    // Register as Singleton or Scoped (destroyed when scope ends)
    builder.Register<SessionInventory>(Lifetime.Singleton);
    builder.RegisterEntryPoint<GameplayEntryPoint>(Lifetime.Singleton);
}
```

#### Injection Methods

**Constructor Injection (Plain C# Classes)**
```csharp
public class MyService
{
    private readonly IMessageHub _messageHub;

    public MyService(IMessageHub messageHub)
    {
        _messageHub = messageHub;
    }
}
```

**Field Injection (MonoBehaviours)**
```csharp
public class MyBehaviour : MonoBehaviour
{
    [Inject] private IMessageHub _messageHub;
}
```

**Method Injection (MonoBehaviours)**
```csharp
public class MyBehaviour : MonoBehaviour
{
    [Inject]
    public void Init(IMessageHub messageHub)
    {
        _messageHub = messageHub;
    }
}
```

---

### 2. State Machine

The **GameStateMachine** manages game flow and major phase transitions.

#### Architecture

```
┌─────────────────────────────────────────────┐
│       GameStateMachine                      │
│  (Registered in BootstrapLifetimeScope)    │
├─────────────────────────────────────────────┤
│ Active State: IState                        │
│ State Dictionary: Type → IState             │
│ MessageHub: IMessageHub (for events)        │
└─────────────────────────────────────────────┘
        │
        ├─→ Enter<TState>()      (transition to state)
        ├─→ Exit()               (exit current state)
        └─→ ActiveState          (get current state)
```

#### Built-in States

| State | Purpose | Transitions |
|-------|---------|-------------|
| **BootstrapState** | Bootstrap initialization | → LoadingState |
| **LoadingState** | Async scene loading | → GameplayState |
| **GameplayState** | Active gameplay | → LoadingState (next level) |
| **MenuState** | Main menu (example) | → GameplayState |

#### State Lifecycle

```csharp
public override void Enter(object payload = null)
{
    // Called when entering the state
    Debug.Log("[MyState] Entered");
}

public override void Exit()
{
    // Called when exiting the state
    Debug.Log("[MyState] Exited");

    // Transition to another state
    StateMachine.Enter<NextState>();
}
```

#### State Transitions & Events

When transitioning states, the state machine publishes events:

```csharp
// Transition request
_stateMachine.Enter<GameplayState>();

// Events published:
// 1. StateExitedEvent(previousState)
// 2. StateEnteredEvent(newState)

// Subscribe to transitions
_messageHub.Subscribe<StateEnteredEvent>(evt =>
{
    Debug.Log($"Entered: {evt.StateType.Name}");
});
```

---

### 3. Event-Driven Communication (MessageHub)

**Easy.MessageHub** provides loosely-coupled pub/sub messaging across the entire game.

#### Global Singleton

MessageHub is registered in BootstrapLifetimeScope and available everywhere via injection.

#### Publishing Events

```csharp
public class MyService
{
    private readonly IMessageHub _messageHub;

    public void DoSomething()
    {
        // Publish an event
        _messageHub.Publish(new MyCustomEvent { Data = 42 });
    }
}
```

#### Subscribing to Events

```csharp
public class MyListener
{
    private readonly IMessageHub _messageHub;

    public MyListener(IMessageHub messageHub)
    {
        _messageHub = messageHub;

        // Subscribe to event
        _messageHub.Subscribe<MyCustomEvent>(OnMyEvent);
    }

    private void OnMyEvent(MyCustomEvent evt)
    {
        Debug.Log($"Received event: {evt.Data}");
    }
}
```

#### Safe Subscription (for MonoBehaviours)

```csharp
public class MyBehaviour : MonoBehaviour
{
    [Inject] private IMessageHub _messageHub;

    [Inject]
    public void Init(IMessageHub messageHub)
    {
        // Safe subscription auto-unsubscribes on object destruction
        _messageHub.SubscribeSafe<MyEvent>(this, OnMyEvent);
    }

    private void OnMyEvent(MyEvent evt) { }
}
```

#### Built-in Events

**State Machine Events**
- `StateEnteredEvent`: Published when entering a state
- `StateExitedEvent`: Published when exiting a state

**Game Events**
- `BalanceLostEvent`: Player lost balance (ragdoll triggered)
- `BalanceRegainedEvent`: Player regained balance
- `PushEvent`: Player pushed another player
- `JumpEvent`: Player jumped
- `InteractEvent`: Player interacted with object
- `ResetBalanceEvent`: Reset player balance

**Inventory Events**
- `ItemAddedEvent`: Item added to inventory
- `ItemRemovedEvent`: Item removed from inventory
- `ItemsDroppedEvent`: Items dropped on impact

---

### 4. Scene Loading & EntryPoints

The project uses VContainer's **EntryPoint** pattern for scene initialization.

#### Scene Flow

```
1. Bootstrap.unity Loads
   ↓
2. BootstrapLifetimeScope.Configure() runs
   - Registers global services
   - Registers BootstrapEntryPoint
   ↓
3. BootstrapEntryPoint.Start() runs (IStartable)
   - Initializes bootstrap systems
   - Calls StateMachine.Enter<BootstrapState>()
   ↓
4. BootstrapState.Enter() runs
   - Loads Main.unity additively
   - Transitions to LoadingState
   ↓
5. Main.unity Loads
   ↓
6. GameplayLifetimeScope.Configure() runs
   - Registers scene services
   - Registers GameplayEntryPoint
   ↓
7. GameplayEntryPoint.Start() runs (IStartable)
   - Initializes gameplay systems
   ↓
8. Game is now playable
```

#### Creating New EntryPoints

```csharp
public class MySceneEntryPoint : IStartable
{
    private readonly IMyService _myService;

    public MySceneEntryPoint(IMyService myService)
    {
        _myService = myService;
    }

    public void Start()
    {
        Debug.Log("[MySceneEntryPoint] Initializing...");
        _myService.Initialize();
    }
}

// Register in LifetimeScope
builder.RegisterEntryPoint<MySceneEntryPoint>(Lifetime.Singleton);
```

---

### 5. Networking (Unity Netcode)

The project integrates **Unity Netcode** for multiplayer functionality.

#### Key Components

| Component | File | Purpose |
|-----------|------|---------|
| **PlayerSpawner** | `Core/Network/PlayerSpawner.cs` | Spawns players when clients connect |
| **PlayerScope** | `Core/Network/PlayerScope.cs` | Per-player DI scope |
| **ClientSidePlayerConfigurator** | `Core/Network/ClientSidePlayerConfigurator.cs` | Client-side player setup |
| **OwnerAuthoritativeNetworkAnimator** | `Core/Player/Animation/OwnerAuthoritativeNetworkAnimator.cs` | Animation sync over network |

#### Network Behavior Pattern

```csharp
public class PlayerController : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // Called when this player spawns on network
        if (IsOwner)
        {
            // Only owner handles input & physics
            SetupOwnerBehavior();
        }
        else
        {
            // Non-owners observe state changes
            SetupObserverBehavior();
        }
    }
}
```

#### RPC Pattern

```csharp
[Rpc(SendTo.ClientsAndHost)]
private void OnBalanceLostRpc(float impactSpeed)
{
    // This method runs on all clients + host
    currentBalanceInfo.IsBalanced = false;
    EnablePlayerRagdoll();
}

// Call from owner
OnBalanceLostRpc(force);
```

---

## Architectural Decisions

### Why Dependency Injection?
- **Testability:** Services can be mocked
- **Decoupling:** Services don't depend on singletons
- **Lifetime Management:** Clear scoping avoids memory leaks
- **Flexibility:** Easy to swap implementations

### Why State Machine?
- **Clear Flow:** Game phase transitions are explicit
- **Event Publishing:** State changes trigger global events
- **Debuggability:** Can log every state transition
- **Separation of Concerns:** Each phase has isolated logic

### Why MessageHub?
- **Loose Coupling:** Components don't need direct references
- **Scalability:** Easy to add new subscribers
- **Flexibility:** Events can have multiple handlers
- **Debugging:** All events flow through one place

### Why VContainer + Netcode?
- **Per-Player Scope:** Each spawned player can have isolated services
- **Clean Networking:** DI handles complex instantiation
- **Consistency:** Same pattern for local and network objects

---

## Common Patterns

### Service Registration Pattern
```csharp
// In LifetimeScope.Configure()
builder.Register<MyService>(Lifetime.Singleton)
    .AsImplementedInterfaces();  // Implements IMyService
```

### Listening to State Changes
```csharp
public class MyUI : MonoBehaviour
{
    [Inject] private IMessageHub _messageHub;

    [Inject]
    public void Init(IMessageHub messageHub)
    {
        _messageHub.Subscribe<StateEnteredEvent>(OnStateEntered);
    }

    private void OnStateEntered(StateEnteredEvent evt)
    {
        if (evt.StateType == typeof(GameplayState))
        {
            // Gameplay started
        }
    }
}
```

### Transitioning Between States
```csharp
public class MyState : BaseState
{
    public override void Enter(object payload = null)
    {
        // Do something...

        // Transition
        StateMachine.Enter<NextState>();
    }
}
```

---

## Architecture Limitations & Trade-offs

| Aspect | Trade-off | Benefit |
|--------|-----------|---------|
| **LifetimeScopes** | More boilerplate code | Clear lifetime management |
| **EntryPoints** | Can't use Awake/Start | Guaranteed DI setup |
| **MessageHub** | Harder to trace calls | Loose coupling |
| **State Machine** | Extra layer of indirection | Clear game flow |
| **VContainer** | Learning curve | Better architecture |

---

## References

- **VContainer Docs:** https://vcontainer.hadashikick.jp/
- **Easy.MessageHub:** https://github.com/Egor92/Easy.MessageHub
- **Unity Netcode:** https://docs-multiplayer.unity3d.com/
- **CLAUDE.md:** Project-specific guidelines (in repo root)
